using UnityEngine;
using UnityEngine.Serialization;

namespace Needle.Engine.Components
{
    [AddComponentMenu(USDZExporter.USDZOnlyMenuPrefix + "Emphasize on Click" + USDZExporter.USDZOnlyMenuTags)]
    public class EmphasizeOnClick : MonoBehaviour
    {
        public GameObject target;
        public float duration = 0.5f;
        // public float moveDistance = 0.5f; // not supported in QuickLook right now
        
        [FormerlySerializedAs("motionType")]
        public MotionType _motionType = MotionType.bounce;
        public string motionType => _motionType.ToString();
        
        private void OnDrawGizmosSelected()
        {
            if (!target || transform == target.transform) return;
            SetActiveOnClick.DrawLineAndBounds(transform, target.transform);
        }
    }

    public enum MotionType
    {
        pop = 0,
        blink = 1,
        bounce = 2,
        flip = 3,
        @float = 4,
        jiggle = 5,
        pulse = 6,
        spin = 7,
    }
}