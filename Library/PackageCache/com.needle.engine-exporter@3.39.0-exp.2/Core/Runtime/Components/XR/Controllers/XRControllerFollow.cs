using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Needle.Engine
{
    [AddComponentMenu("Needle Engine/XR/XR Controller Follow" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
    public class XRControllerFollow : MonoBehaviour
    {
        [Info("Add this script to any GameObject in your scene to make it follow a certain WebXR controller or hand")]
        [Tooltip("The controller to follow (left, right or none)")]
        [JsonIgnore]
        public XRHandedness _side = XRHandedness.left;
        // because in engine we use a string (and not a number) - with this string getter we can still serialize it and have an enum visible in editor
        [UsedImplicitly] public string side => _side == XRHandedness.none ? "none" : _side == XRHandedness.left ? "left" : "right";
        
        [Tooltip("should it follow controllers (the physics controller)")]
        public bool controller = true;
        [Tooltip("should it follow hands (when using hand tracking in WebXR)")]
        public bool hands = true;
        [Tooltip("Disable if you don't want this script to modify the object's visibility.\nIf enabled the object will be hidden when the configured controller or hand is not available.\nIf disabled this script will not modify the object's visibility")]
        public bool controlVisibility = true;
        [Tooltip("When true it will use the grip space, otherwise the ray space")]
        public bool useGripSpace = true;
    }
}
