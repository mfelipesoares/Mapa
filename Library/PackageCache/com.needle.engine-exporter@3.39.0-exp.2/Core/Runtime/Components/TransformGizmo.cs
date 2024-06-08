using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu("Needle Engine/Debug/Transform Gizmo" + Needle.Engine.Constants.NeedleComponentTags)]
    [HelpURL(Constants.DocumentationUrl)]
    public class TransformGizmo : MonoBehaviour
    {
        [Info("When enabled the transform controls will only show up at runtime when the url parameter \"gizmo\" is being used")]
        public bool isGizmo = false;

        [Info("Hold SHIFT to use snapping values:")]
        public uint translationSnap = 1;
        [Range(0,360f)]
        public float rotationSnapAngle = 15;
        public float scaleSnap = .25f;

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.left, -Vector3.left);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(-Vector3.forward, Vector3.forward);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(-Vector3.up, Vector3.up);
        }

        private void OnEnable() { }
    }
}
