using System;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Utils
{
	/// <summary>
	/// Scope assembly lock with nesting support: Unlocks reload if most outer scope is disposed.
	/// </summary>
	public class AssemblyReloadLockScope : IDisposable
	{
		public AssemblyReloadLockScope()
		{
#if UNITY_EDITOR
			EditorApplication.LockReloadAssemblies();
#endif
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			EditorApplication.UnlockReloadAssemblies();
#endif
		}
	}
}