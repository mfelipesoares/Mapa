using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Needle.Engine.Core;
using Needle.Engine.Interfaces;
using Needle.Engine.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Needle.Engine.AdditionalData
{
	[UsedImplicitly]
    public class CameraSkyboxDataEmitter : IBuildStageCallbacks
    {
        public Task<bool> OnBuild(BuildStage stage, ExportContext context)
        {
        	if (stage == BuildStage.PreBuildScene)
	        {
		        var scene = SceneManager.GetActiveScene();
		        var mat = RenderSettings.skybox;
		        if (!scene.IsValid() || !mat || !mat.shader) return Task.FromResult(true);
		        if (mat.shader.name != "Skybox/Better Cubemap (Needle)") return Task.FromResult(true);
		        
		        var cameras = new List<Camera>();
		        ObjectUtils.FindObjectsOfType<Camera>(cameras);
		        foreach (var cam in cameras)
		        {
			        var extra = cam.gameObject.GetComponent<CameraSkyboxData>();
			        if (!extra)
			        {
				        extra = cam.gameObject.AddComponent<CameraSkyboxData>();
				        extra.backgroundBlurriness = mat.GetFloat(CameraSkyboxData.BackgroundBlurriness);
				        extra.backgroundIntensity = mat.GetFloat(CameraSkyboxData.BackgroundIntensity);
				        created.Add(extra);
			        }
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
    }
}