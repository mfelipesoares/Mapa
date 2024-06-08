using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public class NEEDLE_persistent_assets_Importer
	{
		// private readonly Dictionary<string, List<AnimatorState>> clipsToResolve = new Dictionary<string, List<AnimatorState>>();

		private void Register(IImportContext context, int index, Object asset)
		{
			context.Register("/extensions/NEEDLE_persistent_assets/" + index, asset);
		}

		internal void OnImport(IImportContext context, JObject extension)
		{
			// clipsToResolve.Clear();
			const string defaultImportPath = "Assets/Needle/PersistentAssets";
			

			var assets = extension["assets"] as JArray;
			if (assets == null) return;
			for (var index = 0; index < assets.Count; index++)
			{
				var asset = assets[index];
				if (asset == null) continue;
				
				Object instance = default;
				try
				{
					var name = asset.Value<string>("name");
					var assetType = asset.Value<string>("__type");
					var guid = asset.Value<string>("guid");
					if (assetType != null)
					{
						switch (assetType)
						{
							case "AnimatorController":
								string dir;
								if (context.Path != null)
								{
									dir = Path.GetDirectoryName(context.Path);
								}
								else
								{
									dir = defaultImportPath;
									Directory.CreateDirectory(dir);
								}
								
								var path = $"{dir}/{name}.controller";
								var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
								controller.name = name;
								instance = controller;
								Register(context, index, controller);
								context.AddSubAsset(controller);

								// var model = JsonConvert
								// 	.DeserializeObject<AnimatorControllerSerializer.AnimatorControllerModel>(asset.ToString());

								var parameterModels = asset["parameters"] as JArray;
								if (parameterModels != null)
								{
									foreach (var parameterModel in parameterModels)
									{
										var parameter = new AnimatorControllerParameter();
										parameter.name = parameterModel["name"].ToString();
										parameter.type =
											(AnimatorControllerParameterType)parameterModel["type"].Value<int>();
										var value = parameterModel["value"];
										switch (parameter.type)
										{
											case AnimatorControllerParameterType.Bool:
												parameter.defaultBool = value.Value<bool>();
												break;
											case AnimatorControllerParameterType.Float:
												parameter.defaultFloat = value.Value<float>();
												break;
											case AnimatorControllerParameterType.Int:
												parameter.defaultInt = value.Value<int>();
												break;
										}
										controller.AddParameter(parameter);
									}
								}


								// currently we only support one layer
								var stateMachineModel = asset["layers"][0]["stateMachine"] as JObject;
								var statesModel = stateMachineModel["states"] as JArray;
								var defaultStateIndex = 0;
								if (stateMachineModel.TryGetValue("defaultState", out var defaultStateIndexToken))
									defaultStateIndex = defaultStateIndexToken.Value<int>();

								var stateMachine = controller.layers[0].stateMachine;
								var transitionsToResolve = new List<(AnimatorState state, JObject transitionModel)>();


								for (var i = 0; i < statesModel.Count; i++)
								{
									var stateModel = statesModel[i];
									var stateName = stateModel["name"].ToString();
									// . is not allowed in state names
									stateName = stateName.Replace(".", " ");
									var state = stateMachine.AddState(stateName);

									if (i == defaultStateIndex)
									{
										stateMachine.defaultState = state;
									}

									var motionModel = stateModel["motion"];
									if (motionModel != null)
									{
										var isLooping = motionModel["isLooping"].Value<bool>();
										var clipsInfo = motionModel["clips"] as JArray;
										var animationPointer = default(string);
										if (clipsInfo != null)
										{
											animationPointer = clipsInfo[0]["clip"].Value<string>();
										}
										else if (motionModel["clip"] is JValue value)
										{
											animationPointer = value.ToString(CultureInfo.InvariantCulture);
										}

										var resolve = new ResolveReference(context, state, "motion", animationPointer,
											0);
										context.AddCommand(ImportEvent.AfterImport, resolve);

										// register clip to be resolved later
										// if(!clipsToResolve.TryGetValue(animationPointer, out var list))
										// 	clipsToResolve.Add(animationPointer, list = new List<AnimatorState>());
										// list.Add(state);
									}

									if (stateModel["transitions"] is JArray transitions)
									{
										foreach (var transition in transitions)
										{
											transitionsToResolve.Add((state, transition as JObject));
										}
									}
								}

								foreach (var transition in transitionsToResolve)
								{
									var state = transition.state;
									var transitionModel = transition.transitionModel;
									var states = stateMachine.states;
									var destinationState =
										states[transitionModel["destinationState"].Value<int>()].state;
									var transitionInstance = state.AddTransition(destinationState);
									transitionInstance.hasExitTime = transitionModel["hasExitTime"].Value<bool>();
									if (transitionModel.TryGetValue("hasFixedDuration", out var hasFixedDuration))
										transitionInstance.hasFixedDuration = hasFixedDuration.Value<bool>();
									transitionInstance.exitTime = transitionModel["exitTime"].Value<float>();
									transitionInstance.duration = transitionModel["duration"].Value<float>();
									transitionInstance.offset = transitionModel["offset"].Value<float>();

									if (transitionModel.TryGetValue("conditions", out JToken conditionModels))
									{
										foreach (var conditionModel in (JArray)conditionModels)
										{
											var mode = (AnimatorConditionMode)conditionModel["mode"].Value<int>();
											var threshold = conditionModel["threshold"].Value<float>();
											var parameter = conditionModel["parameter"].ToString();
											// TODO: seems like threshold is not correctly serialized in animator? If a bool is false (setting 0) it's still true after import in the AnimatorController transition
											transitionInstance.AddCondition(mode, threshold, parameter);
										}
									}
								}
								break;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					if (instance)
					{
						Object.DestroyImmediate(instance, true);
					}
				}
			}
		}

		internal void OnAfterImport(IImportContext context)
		{
			// foreach (var clip in clipsToResolve)
			// {
			// 	
			// }
		}
	}
}