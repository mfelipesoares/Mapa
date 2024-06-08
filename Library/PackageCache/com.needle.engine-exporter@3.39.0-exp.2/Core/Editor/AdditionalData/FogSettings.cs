using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Interfaces;
using Needle.Engine.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.AdditionalData
{
	public class FogSettings : IBuildStageCallbacks
	{
		
		public Task<bool> OnBuild(BuildStage stage, ExportContext context)
		{
			if (stage == BuildStage.PreBuildScene)
			{
				var exportObject = ObjectUtils.FindObjectOfType<IExportableObject>() as Component;
				var go = default(GameObject);
				if(exportObject) go = exportObject.gameObject;
				else
				{
					go = new GameObject("Fog");
					created.Add(go);
				}
				if (go)
				{
					Create(go);
				}
			}
			else if (stage == BuildStage.PostBuildScene || stage == BuildStage.BuildFailed)
			{
				if (created.Count > 0)
				{
					foreach (var obj in created)
					{
						Object.DestroyImmediate(obj);
					}
					created.Clear();
				}
			}
			
			return Task.FromResult(true);
		}
	
		private static readonly List<Object> created = new List<Object>();
		
		internal static void Create(GameObject go)
		{
			try
			{
				if (go)
				{
					var fog = go.AddComponent<Fog>();
					created.Add(fog);
					fog.hideFlags = HideFlags.HideAndDontSave;
					fog.enabled = RenderSettings.fog;
					fog.mode = RenderSettings.fogMode;
					fog.color = RenderSettings.fogColor;
					fog.density = RenderSettings.fogDensity;
					fog.near = RenderSettings.fogStartDistance;
					fog.far = RenderSettings.fogEndDistance;
				}
			}
			catch(Exception ex)
			{
				// ignored
				Debug.LogWarning("Failed to export fog\n" + ex, go);
			}
		}
	}
}