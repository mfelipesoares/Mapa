using System;
using UnityEngine;

namespace Needle.Engine.Gltf
{
	public static class GltfImporter
	{
		public static event Action<GameObject> AfterImported;
		internal static void RaiseAfterImported(GameObject obj) => AfterImported?.Invoke(obj);
	}
}