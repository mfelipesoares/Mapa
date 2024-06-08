using System;
using System.Collections.Generic;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Needle.Engine.AdditionalData
{
	public class LODData : BaseAdditionalData
	{
		private Camera _mainCamera = null;

		public override void GetAdditionalData(IExportContext context, object instance, List<(object key, object value)> additionalData)
		{
			if (instance is LODGroup lodGroup)
			{
				var lods = lodGroup.GetLODs();

				if (!_mainCamera || _mainCamera.CompareTag("MainCamera") == false)
				{
					this._mainCamera = Camera.main;
					if (!this._mainCamera && Camera.allCameras.Length > 0) this._mainCamera = Camera.allCameras[0];
				}
				if (_mainCamera)
				{
					var models = new List<LodGroupModel>();
					var lastDistance = 0f;
					var lastTransitionHeight = 0f;
					for (var index = 0; index < lods.Length; index++)
					{
						var lod = lods[index];
						var model = new LodGroupModel();
						models.Add(model);
						model.screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight;
						var newDistance = LODUtilityAccess.CalculateDistance(_mainCamera, lastTransitionHeight, lodGroup);
						newDistance *= QualitySettings.lodBias;
						model.distance = newDistance;
						if (float.IsPositiveInfinity(newDistance))
						{
							// and add it to the last distance to get a distance at which to show the last culling state
							model.distance = lastDistance + lastDistance * lastTransitionHeight;
						}
						lastDistance = newDistance;
						lastTransitionHeight = lod.screenRelativeTransitionHeight;
						foreach (var rend in lod.renderers)
						{
							var entry = new JObject();
							entry["guid"] = rend.GetId();
							model.renderers.Add(entry);
						}
					}
					// Add culling state if the last entry screenRelativeTransitionHeight is NOT 0 (it's 0 if its never culled)
					if (lods.Length > 0 && lods[lods.Length - 1].screenRelativeTransitionHeight > 0)
					{
						var lastRelativeHeight = lods[lods.Length - 2].screenRelativeTransitionHeight;
						var dist = lastDistance + lastDistance * lastRelativeHeight;
						dist *= QualitySettings.lodBias;
						models.Add(new LodGroupModel { distance = dist });
					}
					additionalData.Add(("lodModels", models));
				}
			}
		}

		[Serializable]
		public class LodGroupModel
		{
			public float distance;
			public float screenRelativeTransitionHeight;
			public List<JObject> renderers = new List<JObject>();
		}
	}
}