using System;
using UnityEngine;

namespace Needle.Engine.Utils
{
	public static class TransformExtensions
	{
		public static TransformData SaveTransform(this Transform transform, bool local = true)
		{
			if (local)
				return new TransformData(transform.localPosition, transform.localScale, transform.localRotation);
			return new TransformData(transform.position, transform.lossyScale, transform.rotation);
		}

		public static void ApplyTransform(this Transform t, TransformData data, bool local = true)
		{
			if (local)
			{
				t.localPosition = data.SavedPosition;
				t.localRotation = data.SavedRotation;
				t.localScale = data.SavedScale;
			}
			else
			{
				t.position = data.SavedPosition;
				t.rotation = data.SavedRotation;
				t.localScale = data.SavedScale;
			}
		}

		public static void SetLocalIdentity(this Transform t)
		{
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
		}

		public static void SetWorldIdentity(this Transform t)
		{
			t.position = Vector3.zero;
			t.rotation = Quaternion.identity;
			// set world scale to 1
			var ls = t.lossyScale;
			t.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
		}
	}

	public struct TransformData
	{
		public Vector3 SavedPosition, SavedScale;
		public Quaternion SavedRotation;

		public TransformData(Vector3 savedPosition, Vector3 savedScale, Quaternion savedRotation)
		{
			SavedPosition = savedPosition;
			SavedScale = savedScale;
			SavedRotation = savedRotation;
		}
	}

	public readonly struct TransformScope : IDisposable
	{
		private readonly Transform t;
		private readonly TransformData data;
		private readonly bool local;

		public TransformScope(Transform t, bool local = true)
		{
			this.t = t;
			this.data = t.SaveTransform(local);
			this.local = local;
		}

		public void Dispose()
		{
			this.t.ApplyTransform(this.data, local);
		}
	}
}