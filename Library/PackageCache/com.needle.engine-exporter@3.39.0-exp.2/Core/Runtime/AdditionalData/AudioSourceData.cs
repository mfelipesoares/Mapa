using UnityEngine;

namespace Needle.Engine.AdditionalData
{
    public class AudioSourceData : AdditionalComponentData<AudioSource>
    {
        [Tooltip("Will start loading the AudioClip once the AudioSource becomes active (even when playOnAwake is disabled)")]
        public bool preload = false;
    }
}