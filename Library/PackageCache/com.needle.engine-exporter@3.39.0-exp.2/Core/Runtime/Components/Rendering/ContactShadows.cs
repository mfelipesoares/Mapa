using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Needle.Engine
{
    [AddComponentMenu("Needle Engine/Rendering/Contact Shadows" + Needle.Engine.Constants.NeedleComponentTags)]
    public class ContactShadows : MonoBehaviour
    {
        [Tooltip("When enabled the contact shadows will automatically fit the content of the scene at startup")]
        public bool autoFit = false;
        
        [Header("Shadow Settings")]
        [Tooltip("Higher values create stronger shadows with less falloff based on the object shape.")]
        [Range(0.01f,5)]
        public float darkness = 1f;
        [Tooltip("Fade out the shadows. When you provide your own material, this value will be ignored – adjust this on the material instead.")]
        [Range(0,1)]
        public float opacity = 0.5f;
        [Tooltip("Higher values create softer shadows.")]
        public float blur = 4.0f;
        
        [Header("Ground Settings")]
        [Tooltip("Adds a ground occluder mesh. This hides all objects rendering below the shadow plane.")]
        public bool occludeBelowGround = false;
        [Tooltip("If enabled, shadows will also be generated from the backfaces of objects. This is useful if you expect objects to intersect the ground plane.")]
        public bool backfaceShadows = true;
        
        public void fitShadows() {}

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 1, 1, 0.5f);
            var size = Vector3.one;
            size.y = 0;
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.color = new Color(1, 1, 1, 0.1f);
            Gizmos.DrawWireCube(Vector3.zero + Vector3.up * 0.5f, size);
        }
    }
}
