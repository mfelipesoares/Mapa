using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Components
{
    
    [AddComponentMenu("Needle Engine/Networking/Synced Camera" + Needle.Engine.Constants.NeedleComponentTags)]
    [HelpURL(Constants.DocumentationUrl)]
    public class SyncedCamera : MonoBehaviour
    {
        public GameObject cameraPrefab;
    }
}
