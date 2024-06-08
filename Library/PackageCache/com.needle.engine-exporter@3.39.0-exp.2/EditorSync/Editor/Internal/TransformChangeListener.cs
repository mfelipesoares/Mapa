using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine
{
	internal struct TransformData
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;
	}

	internal class TransformChangeListener
	{
		private readonly Editor inspector;
		private readonly PropertyChangedEvent changed;
		private readonly List<Vector3> prevPositions = new List<Vector3>(1), prevScales = new List<Vector3>(1);
		private readonly List<Quaternion> prevRotations = new List<Quaternion>(1);

		internal TransformChangeListener(Editor inspector, PropertyChangedEvent changed)
		{
			this.inspector = inspector;
			this.changed = changed;
		}

		internal void Update()
		{
			if (!this.inspector.IsEnabled()) return;
			if (!this.inspector.serializedObject.isValid) return;
			if (prevPositions.Count != this.inspector.targets.Length)
			{
				foreach (var target in this.inspector.targets)
				{
					var tr = (Transform)target;
					prevPositions.Add(tr.localPosition);
					prevRotations.Add(tr.localRotation);
					prevScales.Add(tr.localScale);
				}
			}
			else
			{
				for (var i = 0; i < this.inspector.targets.Length; i++)
				{
					var target = this.inspector.targets[i];
					if (!target) continue;
					var tr = (Transform)target;
					var prevPosition = prevPositions[i];
					var prevRotation = prevRotations[i];
					var prevScale = prevScales[i];


					if (prevPosition != tr.localPosition)
					{
						var pos = prevPositions[i] = tr.localPosition;
						TransformUtils.ToThreePosition(ref pos);
						this.changed.Invoke(tr, "position", pos);
					}
					if (prevRotation != tr.localRotation)
					{
						var rot = prevRotations[i] = tr.localRotation;
						TransformUtils.ToThreeQuaternion(ref rot);
						this.changed.Invoke(tr, "quaternion", rot);
					}
					if (prevScale != tr.localScale)
					{
						var scale = prevScales[i] = tr.localScale;
						this.changed.Invoke(tr, "scale", scale);
					}
				}
			}
		}
	}
}