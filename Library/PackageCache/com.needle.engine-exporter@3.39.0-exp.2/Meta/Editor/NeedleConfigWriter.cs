using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Needle.Engine.Core;
using Needle.Engine.Interfaces;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
	[UsedImplicitly]
	internal class NeedleConfigWriter : IBuildStageCallbacks
	{
		private static List<JsonConverter> _converters;
		private static List<IBuildConfigProperty> _configSections;
		private static JsonSerializerSettings _serializer;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			ActionsMeta.RequestUpdate += () =>
			{
				var exportInfo = ExportInfo.Get();
				if (exportInfo.Exists())
				{
					UpdateConfig(Path.GetFullPath(exportInfo.GetProjectDirectory()));
				}
			};
		}

		public Task<bool> OnBuild(BuildStage stage, ExportContext context)
		{
			if (stage != BuildStage.Setup) return Task.FromResult(true);
			UpdateConfig(context.ProjectDirectory);
			return Task.FromResult(true);
		}

		public static void UpdateConfig(string projectDirectory)
		{
			_converters ??= NeedleConverter.GetAll();
			_configSections ??= InstanceCreatorUtil.CreateCollectionSortedByPriority<IBuildConfigProperty>();

			if (_serializer == null)
			{
				var converters = new List<JsonConverter>(_converters) { new CopyTextureConverter() };
				_serializer ??= new JsonSerializerSettings() { Converters = converters };
			}

			var sw = new StringWriter();
			var writer = new JsonTextWriter(sw);
			writer.Formatting = Formatting.Indented;
			var serializer = JsonSerializer.Create(_serializer);

			writer.WriteStartObject();
			foreach (var config in _configSections)
			{
				if (string.IsNullOrEmpty(config.Key)) continue;

				var value = config.GetValue(projectDirectory);
				try
				{
					if (value is IList<object> list && list.Any(e => e is Component))
					{
						Debug.LogError("Cannot serialize list of components into needle meta - please report this issue!");
						continue;
					}
					
					writer.WritePropertyName(config.Key);
					
					if (value is Component comp)
					{
						writer.WriteStartObject();
						var serializedObject = new SerializedObject(comp);
						SerializedProperty iterator = serializedObject.GetIterator();
						for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
						{
							if (iterator.propertyPath == "m_Script") continue;
							writer.WriteSerializedProperty(iterator);
						}
						writer.WriteEndObject();
					}
					else
					{
						serializer.Serialize(writer, value);
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					// TODO check if this even works or if the serializer above has already written problematic data at this point
					writer.WriteRaw("undefined");
				}
			}
			writer.WriteEndObject();

			var jsonData = sw.ToString();
			if (NeedleProjectConfig.TryGetCodegenDirectory(out var codegenDirectory))
			{
				Directory.CreateDirectory(codegenDirectory);
				File.WriteAllText(codegenDirectory + "/meta.json", jsonData);
			}
		}
	}
}