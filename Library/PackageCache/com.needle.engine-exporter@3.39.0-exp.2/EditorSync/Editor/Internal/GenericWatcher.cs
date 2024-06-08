using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Needle.Engine
{
	internal class GenericWatcher : IMGUIContainer
	{
		private readonly Editor editor;
		private readonly PropertyChangedEvent evt;
		private readonly string propertyName;
		private readonly Func<object, object> getValue;
		
		private readonly Dictionary<Object, object> previousValues = new Dictionary<object, object>();

		public GenericWatcher(Editor editor, PropertyChangedEvent evt, string propertyName, Func<Object, object> getValue)
		{
			this.editor = editor;
			this.evt = evt;
			this.propertyName = propertyName;
			this.getValue = getValue;
			foreach (var target in this.editor.targets)
			{
				if(!target) previousValues.Add(target, null);
				else previousValues.Add(target, getValue(target));
			}
			onGUIHandler += this.OnGUI;
		}


		private void OnGUI()
		{
			foreach (var target in editor.targets)
			{
				if (!target) continue;
				var prev = previousValues[target];
				var cur = getValue(target);
				if (!Equals(prev, cur))
				{
					previousValues[target] = cur;
					evt?.Invoke(target, this.propertyName, cur);
				}
			}
		}
	}
}