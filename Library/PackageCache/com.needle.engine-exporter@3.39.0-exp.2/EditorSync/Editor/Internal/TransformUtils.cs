
using UnityEngine;

namespace Needle.Engine
{
	public static class TransformUtils
	{
		public static void ToThreePosition(ref Vector3 vec)
		{
			vec.x *= -1;
		}

		public static void ToThreeQuaternion(ref Quaternion quat)
		{
			var fromAxisOfRotation = new Vector3(quat.x, quat.y, quat.z);
			float axisFlipScale = -1;
			var toAxisOfRotation = axisFlipScale * Vector3.Scale(fromAxisOfRotation, new Vector3(-1, 1, 1));
			quat.x = toAxisOfRotation.x;
			quat.y = toAxisOfRotation.y;
			quat.z = toAxisOfRotation.z;
		}
	}
}