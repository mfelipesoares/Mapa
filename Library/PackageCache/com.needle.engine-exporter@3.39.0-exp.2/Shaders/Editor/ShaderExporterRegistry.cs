using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Shaders
{
	public static class ShaderExporterRegistry
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			var assets = AssetDatabase.FindAssets("t:" + nameof(ShaderExportAsset));
			foreach (var asset in assets)
			{
				var instance = AssetDatabase.LoadAssetAtPath<ShaderExportAsset>(AssetDatabase.GUIDToAssetPath(asset));
				if (instance) Register(instance);
			}
		}

		private static readonly List<ShaderExportAsset> exporters = new List<ShaderExportAsset>();

		internal static IReadOnlyList<ShaderExportAsset> Registered => exporters;
		internal static IEnumerable<ShaderExportAsset> ExportCollection => Registered.Where(e => e && e.enabled && e.shader);

		internal static void Register(ShaderExportAsset asset)
		{
			if (exporters.Contains(asset)) return;
			exporters.Add(asset);
		}

		internal static void Unregister(ShaderExportAsset asset)
		{
			exporters.Remove(asset);
		}
        
		[CanBeNull]
		internal static ShaderExportAsset IsMarkedForExport([CanBeNull] UnityEngine.Shader shader, bool enabledOnly = false)
		{
			if (!shader) return null;
			foreach (var exp in exporters)
			{
				if (exp)
				{
					if (exp.shader == shader)
					{
						if (enabledOnly && !exp.enabled) continue;
						return exp;
					}
				}
			}
			return null;
		}

		public static bool HasExportLabel(Object shader)
		{
			return AssetDatabase.GetLabels(shader).Any(l => l.Equals("ExportShader"));
		}

		public static void SetExportLabel(Shader shader, bool enable)
		{
			var currentLabels = AssetDatabase.GetLabels(shader).ToList();
			if (enable)
			{
				if(!currentLabels.Contains("ExportShader"))
					currentLabels.Add("ExportShader");
			}
			else
				currentLabels.Remove("ExportShader");
			AssetDatabase.SetLabels(shader, currentLabels.ToArray());
		}
	}
}