using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine
{
	public class AddRemoveComponentListener : IMGUIContainer
	{
		private readonly GameObjectInspector inspector;
		private readonly PropertyChangedEvent changed;
		private static readonly List<Component> components = new List<Component>();
		private static readonly List<Component> prevComponents = new List<Component>();

		internal AddRemoveComponentListener(GameObjectInspector inspector, PropertyChangedEvent changed)
		{
			this.inspector = inspector;
			this.changed = changed;
			onGUIHandler = OnGUI;
			components.Clear();
			prevComponents.Clear();
			UpdateNow(false);
		}

		private void OnGUI()
		{
			UpdateNow(true);
		}

		private void UpdateNow(bool raiseEvent)
		{
			if (inspector.target is GameObject go)
			{
				if (!go) return;
				
				go.GetComponents(components);

				if (components.Count != prevComponents.Count)
				{
					for (var i = 0; i < prevComponents.Count; i++)
					{
						var prev = prevComponents[i];
						if (!components.Contains(prev))
						{
							// component removed
							if (raiseEvent)
								changed?.Invoke(go, "component:removed", prev);
							break;
						}
					}

					for (var i = 0; i < components.Count; i++)
					{
						var comp = components[i];
						if (!prevComponents.Contains(comp))
						{
							if (raiseEvent)
								changed?.Invoke(go, "component:added", comp);
							break;
						}
					}

					prevComponents.Clear();
					prevComponents.AddRange(components);
				}
			}
		}
	}
}