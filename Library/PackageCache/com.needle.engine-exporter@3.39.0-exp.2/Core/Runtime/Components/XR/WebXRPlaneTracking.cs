using UnityEngine;
using UnityEngine.Serialization;

namespace Needle.Engine
{
    [AddComponentMenu("Needle Engine/XR/Plane Tracking" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
    public class WebXRPlaneTracking : MonoBehaviour
    {
        [FormerlySerializedAs("planeTemplate")]
        [Tooltip("When planes and meshes are detected, this template will be used to create a GameObject for each of them. Can have components and so on.")]
        [Info("Mesh Prefab or GameObject")]
        public GameObject dataTemplate;
        
        [FormerlySerializedAs("initiateRoomCaptureIfNoPlanes")]
        [Tooltip("On Quest, Room Setup can be started automatically if no planes are detected. This is recommended as not everyone has Room Setup completed.")]
        public bool initiateRoomCaptureIfNoData = true;
        
        [Tooltip("Adds plane-detection capabilities to the XR session.")]
        public bool usePlaneData = true;
        
        [Tooltip("Adds mesh-detection capabilities to the XR session.")]
        public bool useMeshData = true;

        [Tooltip("If enabled plane or mesh tracking will also be running in VR mode")]
        public bool runInVR = true;
    }
}
