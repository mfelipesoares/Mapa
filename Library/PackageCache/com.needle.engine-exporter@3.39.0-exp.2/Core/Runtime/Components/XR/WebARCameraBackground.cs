using UnityEngine;

namespace Needle.Engine.Components
{
    
    [AddComponentMenu("Needle Engine/XR/WebAR Camera Background" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
    public class WebARCameraBackground : MonoBehaviour
    {
        public Color backgroundTint = new Color(1, 1, 1, 1);
    }
}
