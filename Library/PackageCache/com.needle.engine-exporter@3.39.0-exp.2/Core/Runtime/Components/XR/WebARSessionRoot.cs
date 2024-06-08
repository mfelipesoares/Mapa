using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu("Needle Engine/XR/WebAR Session Root" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
    [HelpURL(Constants.DocumentationUrl)]
    public class WebARSessionRoot : MonoBehaviour
    {
        [Info("User scale. Typically, will be greater than one, so that the AR user is bigger and looks at a small scene.")]
        [Tooltip("User scale. Typically, will be greater than one, so that the AR user is bigger and looks at a small scene.")]
        public float arScale = 1;
        public bool invertForward = false;
        [Tooltip("Assign a prefab or an object in your scene to be used as custom placement indicator.")]
        public Transform customReticle;
        [Tooltip("Experimental: Enable touch input to transform the scene in AR (move with one finger, scale and rotate with two fingers)")]
        public bool arTouchTransform = false;
        [Tooltip("Experimental: When enabled the scene will automatically be placed when a point in the real world is found for the scene to be placed. No user interaction required.")]
        public bool autoPlace = false;
        [Tooltip("Experimental: When enabled an XRAnchor will be created for the AR scene and the position will be updated to the anchor position every few frames")]
        public bool useXRAnchor = false;
        
    }
}
