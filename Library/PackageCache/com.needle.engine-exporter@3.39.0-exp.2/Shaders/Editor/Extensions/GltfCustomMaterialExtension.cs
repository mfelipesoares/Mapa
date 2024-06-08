using System.Collections.Generic;
using Needle.Engine.Gltf;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if !UNITY_2021_1_OR_NEWER
using System.Linq;
#endif

namespace Needle.Engine.Shaders.Extensions
{
	/// <summary>
	/// Used to export every shader with every configuration only once
	/// </summary>
	public readonly struct ShaderExportConfiguration
	{
		// public readonly Material Material; < never use the material as key here since this is just the settings of the shader and not what properties the material has nor which material instance is using this shader
		public readonly Shader Shader;
		public readonly int SubShaderIndex;
		public readonly int PassIndex;
		public readonly ShaderExporter.Mode Mode;
		public readonly string Keywords;

		public ShaderExportConfiguration(ShaderExportAsset asset, Material material)
		{
			// this.Material = material;
			#if UNITY_2021_1_OR_NEWER
			this.Keywords = string.Join(";", material.enabledKeywords);
			#else
			this.Keywords = string.Join(";", material.shaderKeywords.Select(material.IsKeywordEnabled));
			#endif
			if (asset)
			{
				this.Shader = asset.shader;
				this.SubShaderIndex = asset.subshaderIndex;
				this.PassIndex = asset.passIndex;
				this.Mode = asset.mode;
			}
			else
			{
				this.Shader = material.shader;
				this.SubShaderIndex = 0;
				this.PassIndex = 0;
				this.Mode = ShaderExporter.Mode.WebGL2;
			}
		}
	}

	public abstract class GltfCustomMaterialExtensionHandler : GltfExtensionHandlerBase
	{
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			shaders.Clear();
			shaderTechniqueMap.Clear();
			exportedShaders.Clear();
		}

		protected readonly ExtensionData shaders = new ExtensionData();

		private readonly Dictionary<Material, int> shaderTechniqueMap = new Dictionary<Material, int>();
		private readonly ShaderExporter shaderExporter = new ShaderExporter();
		private readonly ThreeShaderPostProcessor postProcessor = new ThreeShaderPostProcessor();

		private readonly Dictionary<ShaderExportConfiguration, int> exportedShaders =
			new Dictionary<ShaderExportConfiguration, int>();

		private static ProfilerMarker _marker = new ProfilerMarker("Needle Export Custom Shader");

		public bool TryGetTechniqueIndex(Material mat, out int index)
		{
			if (!mat)
			{
				index = -1;
				return false;
			}
			return shaderTechniqueMap.TryGetValue(mat, out index);
		}

		public override void OnAfterMaterialExport(GltfExportContext context, Material material, int materialId)
		{
			base.OnAfterMaterialExport(context, material, materialId);
			var shader = material.shader;
			// FIXME: this does currently return the first asset that has this shader 
			// so if we have multiple assets having different configs for the same shader 
			// we cant do that right now and only the first one will be used (e.g. export pass 0 and pass 1)
			// SOME of the code below already assumes it is possible but for some cases we also need other info
			// e.g. context for where do we want to use which shader pass then...
			var exporter = ShaderExporterRegistry.IsMarkedForExport(shader);
			var markedForExport = exporter && exporter.enabled;

			if (!markedForExport) markedForExport |= ShaderExporterRegistry.HasExportLabel(shader);
			if (!markedForExport) markedForExport |= ShaderExporterRegistry.HasExportLabel(material);

			if (!markedForExport) return;

			// make sure we export every ShaderAsset with every configuration only once
			var key = new ShaderExportConfiguration(exporter, material);
			if (exportedShaders.TryGetValue(key, out var techniqueIndex))
			{
				if (!shaderTechniqueMap.ContainsKey(material))
					this.shaderTechniqueMap.Add(material, techniqueIndex);
				return;
			}
			
			using (_marker.Auto())
			{
				// compile the shader to get technique, programs, uniforms
				shaderExporter.Clear();
				shaderExporter.Add(material);
				shaderExporter.GetData(shader, key.SubShaderIndex, key.PassIndex, key.Mode, out var data);
				if (data == null || data.techniques.Count <= 0)
				{
					Debug.LogError("Custom shader export failed - didnt get techniques: " + shader, material);
					return;
				}
				postProcessor.PostProcessShaders(data);
				techniqueIndex = RegisterMaterial(material);
				exportedShaders.Add(key, techniqueIndex);
				AddShader(material, data);
				
				// For debugging
				// for (int i = 0; i < data.shaders.Count; i++)
				// 	File.WriteAllText("Assets/" + material.name + "_s" + i + "_type" + data.shaders[i].type + ".txt", data.shaders[i].code);
			}
		}

		/// <summary>
		/// adds the shader to a single shader techniques, programs, uniforms files as defined in the spec
		/// https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Archived/KHR_techniques_webgl#extension
		/// It inserts and updates the new indices accordingly
		/// </summary>
		private void AddShader(Material mat, ExtensionData newData)
		{

			var technique = newData.techniques[0];
			this.shaders.techniques.Add(technique);

			var program = newData.programs[0];
			technique.program = this.shaders.programs.Count;
			program.id = technique.program;
			this.shaders.programs.Add(program);

			var frag = newData.shaders[program.fragmentShader];
			program.fragmentShader = this.shaders.shaders.Count;
			this.shaders.shaders.Add(frag);

			var vert = newData.shaders[program.vertexShader];
			program.vertexShader = this.shaders.shaders.Count;
			this.shaders.shaders.Add(vert);
		}

		private int RegisterMaterial(Material mat)
		{
			var techniqueIndex = this.shaders.techniques.Count;
			// technically we would need to save the whole config here
			// e.g. when a shader is exported for multiple passes
			// but then we would need to know that info when serializing the material extension too
			this.shaderTechniqueMap.Add(mat, techniqueIndex);
			return techniqueIndex;
		}
	}
}