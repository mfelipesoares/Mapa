using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Core.References;
using Needle.Engine.Interfaces;
using Needle.Engine.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Needle.Engine.Core.Emitter
{
	[Priority(1000)]
	[UsedImplicitly]
	public class GltfEmitter : IEmitter
	{
		internal static Action<GltfEmitter, Component, ExportContext> BeforeRun;
		
		public GltfEmitter()
		{
			Builder.BuildStarting += OnBuildStart;
		}

		private static void OnBuildStart()
		{
			didCollectGLTFsInScene = false;
		}

		private static bool didCollectGLTFsInScene = false;
		private static readonly List<IExportableObject> exportableObjectsInScene = new List<IExportableObject>();
		private void EnsureGLTFsInSceneAreCollected()
		{
			if (didCollectGLTFsInScene) return;
			didCollectGLTFsInScene = true;
			exportableObjectsInScene.Clear();
			ObjectUtils.FindObjectsOfType(exportableObjectsInScene);
			// exportableObjectsInScene.RemoveAll(ex =>
			// {
			// 	if (ex is Component comp && comp.gameObject.activeInHierarchy == false) return true;
			// 	return false;
			// });
		}

		internal static void WriteExportedFilePath(IWriter writer, ExportContext context, string path)
		{
			var baseUrl = context.BaseUrl;
			if (!string.IsNullOrEmpty(baseUrl))
			{
				var baseUrlPath = baseUrl + "/" + Path.GetFileName(path) + "?v=" + context.Hash;
				writer.Write($"needle_exported_files.push(\"{baseUrlPath}\");");
			}
			else
			{
				var projectDirectory = context.Project.ProjectDirectory;
				var proj = new Uri(projectDirectory + "/", UriKind.Absolute);
				var filePath = new Uri(path, UriKind.Absolute);
				var loadPath = proj.MakeRelativeUri(filePath) + "?v=" + context.Hash;
				writer.Write($"needle_exported_files.push(\"{loadPath}\");");
			}
		}
		
		public ExportResultInfo Run(Component comp, ExportContext context)
		{
			if (context.IsInGltf) return ExportResultInfo.Failed;
			if (!comp.TryGetComponent(out IExportableObject gltf)) return ExportResultInfo.Failed;
			// if the GltfObject is not a nested gltf and disabled, we skip it
			if (comp.gameObject.activeInHierarchy == false && context.ParentContext == null)
			{
				if(ReferenceEquals(comp, gltf)) Debug.Log($"Ignoring disabled glTF object \"{comp.name}\" because no active parent glTF object was found. If this is unintentional you may either put this object in your active hierarchy or make sure to add a toplevel glTF object to your scene and add everything in your scene that should be exported as a child to it.".LowContrast(), comp);
				return ExportResultInfo.Failed;
			}
			BeforeRun?.Invoke(this, comp, context);

			context.IsInGltf = true;

			var watch = new Stopwatch();
			watch.Start();

			// TODO: make sure child object names are unique (gltf will bake them unique but we need to know their exported name to emit the correct find variable name)

			var outputDirectory = context.Project.AssetsDirectory;

			// ReSharper disable once UnusedVariable
			// using var util = new RenameUtil(context, comp.gameObject);

			var fileName = gltf.name;
			ReferenceExtensions.ToJsVariable(ref fileName);
			fileName = $"{fileName}{context.GetExtension(comp.gameObject)}";
			var path = $"{outputDirectory}/{fileName}";
			var didExport = gltf.Export(path, false, context);

			var writer = context.Writer;
			string id = default;
			if (gltf is Object obj) id = obj.GetId();
			var variableName = $"{gltf.name}_{id}";
			ReferenceExtensions.ToJsVariable(ref variableName);

			EnsureGLTFsInSceneAreCollected();
			// This array is declared in Builder
			WriteExportedFilePath(writer, context, path);

			watch.Stop();
			if (didExport)
				Debug.Log($"<b>Exported</b> {fileName} in {watch.Elapsed.TotalMilliseconds:0} ms", comp);

			return new ExportResultInfo(path, true);
		}
	}
}