using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Needle.Engine
{
    
    [AddComponentMenu("Needle Engine/Export/GLTFExport" + Needle.Engine.Constants.NeedleComponentTags)]
    public class GltfExport : MonoBehaviour
    {
        [Tooltip("Exports .glb when set to true and a zipped .gltf when set to false.")]
        public bool binary = true;
        [Tooltip("Leave empty to export all objects in the scene.")]
        public List<GameObject> objects = new List<GameObject>();

        public void ExportNow(string name = null) { }
    }
}
