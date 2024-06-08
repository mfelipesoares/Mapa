using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using JetBrains.Annotations;
using Needle.Engine.Core;
using Needle.Engine.Problems;
using Needle.Engine.Utils;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf
{
	[UsedImplicitly]
	public class AnimatorControllerHandler : GltfExtensionHandlerBase
	{
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			context.RegisterValueResolver(new AnimatorControllerSerializer());
		}
	}

	internal class AnimatorControllerSerializer : IValueResolver
	{
		public bool TryGetValue(IExportContext ctx, object instance, MemberInfo member, ref object value)
		{
			var bridge = (ctx as GltfExportContext)?.Bridge;
			
			if (value is AnimatorController ctr)
			{
				var transform = (instance as Component)?.transform;
				value = new AnimatorControllerModel(ctr, bridge, transform, ctx);
				return true;
			}
			//
			if (value is AnimatorOverrideController)
			{
				value = null;
				return true;
			}
			return false;
		}

		[Serializable]
		public class AnimatorControllerModel : ISerializablePersistentAssetModel
		{
			[NonSerialized] private readonly AnimatorController controller;

			[NonSerialized] private List<MotionModel> motions = new List<MotionModel>();

			public void OnNewObjectDiscovered(Object asset, object owner, MemberInfo member, IExportContext context)
			{
				var bridge = (context as GltfExportContext)?.Bridge;
				if (bridge != null && asset == controller && owner is Component component)
				{
					var transform = component.transform;
					foreach (var motion in motions)
					{
						if (motion._clip == null) continue;
						bridge.AddAnimationClip(motion._clip, transform, 1);
						var mapping = motion.clips;
						var newMapping = new ClipNodeMapping(motion._clip, bridge, transform);
						mapping.Add(newMapping);
					}
				}
			}

			public AnimatorControllerModel(AnimatorController controller, IGltfBridge bridge, Transform transform, IExportContext context)
			{
				this.controller = controller;
				name = controller.name;
				guid = controller.GetId();

				foreach (var param in controller.parameters)
				{
					var p = new ParameterModel();
					parameters.Add(p);
					p.name = param.name;
					p.type = param.type;
					p.hash = Animator.StringToHash(param.name);

					switch (param.type)
					{
						case AnimatorControllerParameterType.Float:
							p.value = param.defaultFloat;
							break;
						case AnimatorControllerParameterType.Int:
							p.value = param.defaultInt;
							break;
						case AnimatorControllerParameterType.Bool:
							p.value = param.defaultBool;
							break;
						case AnimatorControllerParameterType.Trigger:
							p.value = param.defaultBool;
							break;
					}
				}
				
				// const int k_maxStatesAfterTrial = 2;
				// var trialHasEnded = NeedleEngineAuthorization.TrialEnded;
				// var isInTrial = NeedleEngineAuthorization.IsInTrialPeriod;
				var totalStates = 0;

				// Export layers
				foreach (var layer in controller.layers)
				{
					var layerObj = new LayerModel();
					layerObj.name = layer.name;
					layers.Add(layerObj);

					var stateMachine = layer.stateMachine;
					var stateMachineModel = new StateMachineModel();
					layerObj.stateMachine = stateMachineModel;
					stateMachineModel.name = stateMachine.name;

					// if (isInTrial && !LicenseCheck.HasLicense)
					// {
					// 	if (stateMachine.states.Length > k_maxStatesAfterTrial)
					// 	{
					// 		var msg =
					// 			$"AnimatorController {controller.name} has more than {k_maxStatesAfterTrial} states. Upgrade to an Indie or Pro Plan of Needle to export more than {k_maxStatesAfterTrial} states after your Pro trial has ended.";
					// 		Debug.LogWarning(msg, controller);
					// 		BuildResultInformation.ReportBuildProblem(msg, controller, LicenseType.Indie);
					// 	}
					// }
					
					for (var index = 0; index < stateMachine.states.Length; index++)
					{
						var stateEntry = stateMachine.states[index];

						// if (LicenseCheck.HasLicense == false)
						// {
						// 	if (trialHasEnded && totalStates >= k_maxStatesAfterTrial)
						// 	{
						// 		var message =
						// 			$"AnimatorController {controller.name} has more than {k_maxStatesAfterTrial} states. Upgrade to an Indie or Pro Plan of Needle to export AnimatorControllers with more than {k_maxStatesAfterTrial} animation states.";
						// 		Debug.LogWarning(message, controller);
						// 		var info = new BuildResultInformation($"AnimatorController \"{controller.name}\" has {stateMachine.states.Length} animation states", controller, ProblemSeverity.Error);
						// 		info.ActionDescription = "<b>Purchase a commercial license</b> or reduce to 2 states";
						// 		BuildResultInformation.Report(info);
						// 		break;
						// 	}
						// }
						
						totalStates += 1;
						
						var state = stateEntry.state;
						var stateModel = new StateModel();
						stateMachineModel.states.Add(stateModel);
						stateModel.name = state.name;
						stateModel.hash = Animator.StringToHash(state.name);
						stateModel.speed = state.speed;
						if (state.speedParameterActive)
							stateModel.speedParameter = state.speedParameter;

						state.cycleOffset = state.cycleOffset;
						if (state.cycleOffsetParameterActive) 
							stateModel.cycleOffsetParameter = state.cycleOffsetParameter;

						if (state == stateMachine.defaultState)
							stateMachineModel.defaultState = index;

						var motion = state.motion;
						if (motion)
						{
							var motionModel = new MotionModel();
							motions.Add(motionModel);
							stateModel.motion = motionModel;
							motionModel.name = motion.name;
							motionModel.isLooping = motion.isLooping;
							motionModel.guid = motion.GetId();
							switch (motion)
							{
								case AnimationClip clip:	                                    
									// clips.Add(controller, clip);
									if (context is GltfExportContext ctx)
									{
										ctx.Bridge.AddAnimationClip(clip, transform, 1);
									}
									
									motionModel._clip = clip;

									var clipMapping = new ClipNodeMapping(clip, bridge, transform);
									motionModel.clips.Add(clipMapping);
									break;
							}
						}


						foreach (var behaviour in state.behaviours)
						{
							if (context.TypeRegistry.IsInstalled(behaviour.GetType()))
								stateModel.behaviours.Add(new StateMachineBehaviourModel(behaviour));
						}

						if (stateMachine.anyStateTransitions != null)
						{
							// ReSharper disable once CoVariantArrayConversion
							var anyStateTransitions = CreateTransitionsArray(true, stateMachine.anyStateTransitions, stateMachine.states);
							stateModel.transitions.AddRange(anyStateTransitions);
						}
						
						// ReSharper disable once CoVariantArrayConversion
						stateModel.transitions.AddRange(CreateTransitionsArray(false, state.transitions, stateMachine.states));


					}
					// ReSharper disable once CoVariantArrayConversion
					stateMachineModel.entryTransitions.AddRange(CreateTransitionsArray(false, stateMachine.entryTransitions, stateMachine.states));
				}
			}


			private static IEnumerable<TransitionModel> CreateTransitionsArray(bool isAny, AnimatorTransitionBase[] transitions, ChildAnimatorState[] states)
			{
				foreach (var transition in transitions)
				{
					var transitionModel = new TransitionModel();
					transitionModel.isExit = transition.isExit;
					// transitionModel.isAny = isAny;

					if (transition is AnimatorStateTransition stateTransition)
					{
						transitionModel.exitTime = stateTransition.exitTime;
						transitionModel.hasFixedDuration = stateTransition.hasFixedDuration;
						transitionModel.offset = stateTransition.offset;
						transitionModel.duration = stateTransition.duration;
						transitionModel.hasExitTime = stateTransition.hasExitTime;
					}

					// find destination state index
					var found = false;
					var destStates = transition.destinationStateMachine?.states ?? states;
					for (var i = 0; i < destStates.Length; i++)
					{
						if (found) break;
						var dest = destStates[i].state;
						if (dest != transition.destinationState) continue;
						transitionModel.destinationState = i;
						found = true;
					}
					if (!found)
						transitionModel.destinationState = -1;


					transitionModel.conditions = new List<ConditionModel>();
					foreach (var condition in transition.conditions)
					{
						var conditionModel = new ConditionModel();
						conditionModel.parameter = condition.parameter;
						conditionModel.mode = condition.mode;
						conditionModel.threshold = condition.threshold;
						transitionModel.conditions.Add(conditionModel);
					}

					yield return transitionModel;
				}
			}

			public string name;
			public string guid;
			public List<ParameterModel> parameters = new List<ParameterModel>();
			public List<LayerModel> layers = new List<LayerModel>();

			[Serializable]
			public class ParameterModel
			{
				public string name;
				public AnimatorControllerParameterType type;
				public int hash;
				public object value;
			}

			[Serializable]
			public class LayerModel
			{
				public string name;
				public StateMachineModel stateMachine = new StateMachineModel();
			}

			[Serializable]
			public class StateMachineModel
			{
				public string name;
				public int defaultState;
				public List<StateModel> states = new List<StateModel>();
				public List<TransitionModel> entryTransitions = new List<TransitionModel>();
			}

			[Serializable]
			public class StateModel
			{
				public string name;
				public int hash;
				public MotionModel motion;
				public List<TransitionModel> transitions = new List<TransitionModel>();
				public List<StateMachineBehaviourModel> behaviours = new List<StateMachineBehaviourModel>();
				public float speed;
				public string speedParameter;
				public float cycleOffset;
				public string cycleOffsetParameter;
			}

			[Serializable]
			public class MotionModel
			{
				public string name;
				public bool isLooping;
				public string guid;

				/// <summary>
				/// Used if multiple animators use the same animator controller
				/// We dont need to copy the whole controller in the extension
				/// But instead can store the clip id / pointer per transform id (node id)
				/// </summary>
				public List<ClipNodeMapping> clips = new List<ClipNodeMapping>();

				[NonSerialized] [CanBeNull] public AnimationClip _clip;
			}

			public class ClipNodeMapping
			{
				public readonly string node;
				public readonly string clip;

				public ClipNodeMapping(AnimationClip clip, IGltfBridge bridge, Transform transform)
				{
					this.node = bridge.TryGetNodeId(transform).AsNodeJsonPointer();
					this.clip = bridge?.TryGetAnimationId(clip, transform).AsAnimationPointer();
				}
			}

			[Serializable]
			public class TransitionModel
			{
				public bool isExit;
				public float exitTime;
				public bool hasFixedDuration;
				public float offset;
				public float duration;
				public bool hasExitTime;
				public int destinationState;
				public List<ConditionModel> conditions = new List<ConditionModel>();
				public bool isAny;
			}

			[Serializable]
			public class ConditionModel
			{
				public string parameter;
				public AnimatorConditionMode mode;
				public float threshold;
			}
			
			[Serializable]
			public class StateMachineBehaviourModel
			{
				public string typeName;
				public StateMachineBehaviour properties;

				public StateMachineBehaviourModel(StateMachineBehaviour stateMachineBehaviour)
				{
					typeName = stateMachineBehaviour.GetType().Name;
					properties = stateMachineBehaviour;
				}
			}
		}
	}
}