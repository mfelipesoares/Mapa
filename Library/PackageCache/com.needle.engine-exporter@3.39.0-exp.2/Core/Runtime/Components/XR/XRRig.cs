using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu("Needle Engine/XR/XR Rig" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
    [HelpURL(Constants.DocumentationUrl)]
    public class XRRig : MonoBehaviour
    {
        [Info("At start the XRRig with the highest priority will be chosen")]
        [Tooltip("At start the XRRig with the highest priority will be chosen")]
        public int priority;

        /** Call to set this XR rig as the active rig (during a running XR session) */
        public void setAsActiveXRRig() {}
        
        private void OnDrawGizmos()
        {
            RenderRigGizmos(transform.localToWorldMatrix);
        }

        internal static void RenderRigGizmos(Matrix4x4 localToWorldMatrix, bool implicitRig = false)
        {
            #if UNITY_EDITOR
            Gizmos.matrix = localToWorldMatrix;
            Handles.matrix = localToWorldMatrix;
            var forwardColor = implicitRig ? Color.gray : Color.blue;
            Gizmos.color = forwardColor;
            Handles.color = forwardColor;

            var headHeight = 1.5f;
            var size = new Vector3(1, 1.7f, 1);
            var pos = new Vector3(0, headHeight, 0);
            Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(Vector3.forward), .2f, EventType.Repaint);

            Gizmos.color = implicitRig ? Color.gray : Color.green;
            Gizmos.DrawWireCube(new Vector3(0, size.y * .5f, 0), size);
            Gizmos.color = forwardColor;
            Gizmos.DrawWireSphere(new Vector3(0, headHeight, 0), .05f);
            #endif
        }
    }
}
