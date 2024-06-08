using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public interface IImportContext
	{
		[CanBeNull] string Path { get; }
		IGltfImporterBridge Bridge { get; }

		void Register(string key, object asset);
		bool TryResolve(string key, out object asset);

		void AddSubAsset(Object obj);

		void AddCommand(ImportEvent evt, ICommand cmd);
	}

	internal class NeedleGltfImportContext : IImportContext
	{
		private readonly AssetImportContext assetImport;
		public string Path { get; }
		public IGltfImporterBridge Bridge { get; }

		public NeedleGltfImportContext(string path, IGltfImporterBridge bridge, AssetImportContext assetImport)
		{
			this.assetImport = assetImport;
			this.Path = path;
			Bridge = bridge;
		}


		private readonly Dictionary<string, object> references = new Dictionary<string, object>();

		public void Register(string key, object asset)
		{
			if (!references.ContainsKey(key)) references.Add(key, asset);
		}

		public bool TryResolve(string key, out object asset)
		{
			return references.TryGetValue(key, out asset);
		}

		internal List<Object> SubAssets { get; } = new List<Object>();

		public void AddSubAsset(Object obj)
		{
			if (assetImport != null)
				assetImport.AddObjectToAsset(obj.name, obj);
			else SubAssets.Add(obj);
		}


		private readonly Dictionary<ImportEvent, List<ICommand>> commandList =
			new Dictionary<ImportEvent, List<ICommand>>();

		public void AddCommand(ImportEvent evt, ICommand cmd)
		{
			if (!commandList.ContainsKey(evt)) commandList.Add(evt, new List<ICommand>());
			commandList[evt].Add(cmd);
		}

		internal async Task ExecuteCommands(ImportEvent evt)
		{
			if (commandList.TryGetValue(evt, out var commands) && commands != null)
			{
				var taskList = new List<Task>();
				for (var index = 0; index < commands.Count; index++)
				{
					var p = commands[index];
					var task = p.Execute();
					taskList.Add(task);
				}
				commands.Clear();
				await Task.WhenAll(taskList);
			}
		}
	}
}