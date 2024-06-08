using UnityEngine;

namespace Needle.Engine
{
	public enum PhysicsEngine
	{
		None = 0,
		Auto = 1,
		Rapier = 2,
	}
	
	public class NeedleEngineModules : MonoBehaviour
	{
		public PhysicsEngine PhysicsEngine = PhysicsEngine.Auto;
	}
	
	internal class PhysicsConfig : IBuildConfigProperty
	{
		public string Key => "useRapier";
		
		public object GetValue(string projectDirectory)
		{
			var mod = Object.FindAnyObjectByType<NeedleEngineModules>();
			if (mod)
			{
				if (mod.PhysicsEngine == PhysicsEngine.Auto)
				{
					if(UsePhysicsAuto()) return true;
				}
				
				return mod.PhysicsEngine == PhysicsEngine.Rapier;
			}
			return true;
		}

		public static bool UsePhysicsAuto()
		{
			// This doesnt check if any referenced prefab or scene has a Collider or RigidBody.
			// It could also fail when physical objects are disabled in the scene and expected to later be enabled.
			if (Object.FindAnyObjectByType<Collider>()) return true;
			if (Object.FindAnyObjectByType<Rigidbody>()) return true;
			return false;
		}
	}
}