using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;

namespace Needle.Engine.UniversalRenderPipeline
{
	internal class VolumeChangeTracker
	{
		private readonly Volume volume;
		private readonly PropertyChangedEvent objPropertyChangedEvent;
		private bool isSelected;

		public VolumeChangeTracker(Volume volume, PropertyChangedEvent objPropertyChangedEvent)
		{
			this.volume = volume;
			this.objPropertyChangedEvent = objPropertyChangedEvent;
		}

		public void Validate()
		{
			if (!volume) isSelected = false;
			else isSelected = Selection.activeGameObject == volume.gameObject;
		}

		public void Update()
		{
			if (!isSelected) return;
			if (!volume) return;

			var profile = volume.sharedProfile;
			if (profile)
			{
				for (var i = 0; i < profile.components.Count; i++)
				{
					var comp = profile.components[i];
					TestIfComponentActiveStateChanged(ref _previousActiveStates, comp, i, profile.components.Count);
					if (!comp.active)
					{
						continue;
					}
					var count = comp.parameters.Count;
					for (var index = 0; index < count; index++)
					{
						var param = comp.parameters[index];
						TestIfComponentParameterActiveStateChanged(comp, param, index, count);
						if (param.overrideState == false) continue;
						switch (param)
						{
							case IntParameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case FloatParameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case BoolParameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case ColorParameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case FloatRangeParameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case Vector2Parameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case Vector3Parameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							case Vector4Parameter par:
								TestIfValueHasChanged(comp, index, par.value, count);
								break;
							default:
								if (!_volumeParameterFieldCache.TryGetValue(param.GetType(), out var field))
								{
									field = param.GetType().GetField("m_Value",
										BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
									_volumeParameterFieldCache.Add(param.GetType(), field);
								}
								if (field == null) break;
								var value = field.GetValue(param);
								if(value != null)
									TestIfValueHasChanged(comp, index, value, count);
								break;
						}
					}
				}
			}
		}

		private static readonly Dictionary<Type, FieldInfo> _volumeParameterFieldCache = new Dictionary<Type, FieldInfo>();

		private bool[] _previousActiveStates;

		private void TestIfComponentActiveStateChanged(ref bool[] states, VolumeComponent comp, int index, int count)
		{
			var value = comp.active;
			if (states == null || states.Length != count)
			{
				states = new bool[count];
				states[index] = value;
				return; 
			}
			var prev = states[index];
			var changed = prev != value;
			if (changed)
			{
				states[index] = value;
				var componentName = comp.GetType().Name;
				var path = "postprocessing." + componentName + ".active";
				path = path.ToLower();
				objPropertyChangedEvent.Invoke(volume, path, value);
			}
		}


		private readonly Dictionary<int, bool?[]> _previousParameterActiveStates = new Dictionary<int, bool?[]>();

		private void TestIfComponentParameterActiveStateChanged(VolumeComponent comp, VolumeParameter parameter, int index, int count)
		{
			var value = parameter.overrideState;
			if (!_previousParameterActiveStates.TryGetValue(comp.GetInstanceID(), out var states) || states.Length != count)
			{
				states = new bool?[count];
				_previousParameterActiveStates.Add(comp.GetInstanceID(), states);
			}
			
			var prev = states[index];
			var changed = prev != value;
			if (changed)
			{
				states[index] = value;
				if (prev != null)
				{
					var componentName = comp.GetType().Name;
					var name = GetVolumeComponentParameterName(comp, index);
					var path = "postprocessing." + componentName + "." + name + ".active";
					path = path.ToLower();
					objPropertyChangedEvent.Invoke(volume, path, value);
				}
			}
		}

		
		
		private readonly Dictionary<int, object[]> _previousValues =
			new Dictionary<int, object[]>();

		private void TestIfValueHasChanged(VolumeComponent comp, int parameterIndex, object value, int maxParams)
		{
			if (!_previousValues.TryGetValue(comp.GetInstanceID(), out var values))
			{
				values = new object[maxParams];
				values[parameterIndex] = value;
				_previousValues.Add(comp.GetInstanceID(), values);
				return;
			}
			var prev = values[parameterIndex];
			var changed = !Equals(value, prev);

			if (changed)
			{
				values[parameterIndex] = value;
				if (prev != null)
				{
					var name = GetVolumeComponentParameterName(comp, parameterIndex);
					var componentName = comp.GetType().Name;
					var path = "postprocessing." + componentName + "." + name;
					path = path.ToLower();
					objPropertyChangedEvent.Invoke(volume, path, value);
				}
			}
		}

		private static readonly Dictionary<Type, string[]> parameterNames = new Dictionary<Type, string[]>();

		private static string GetVolumeComponentParameterName(VolumeComponent comp, int index)
		{
			if (!parameterNames.TryGetValue(comp.GetType(), out var names))
			{
				var type = comp.GetType();
				// Same code as VolumeComponent in OnEnable to ensure we have the same order too
				names = type
					.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
					.OrderBy(t => t.MetadataToken) // Guaranteed order
					.Select(t => t.Name)
					.ToArray();
				parameterNames.Add(type, names);
			}

			if (names.Length <= index) return null;
			return names[index];
		}
	}
}