using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Play Audio on Click" + USDZExporter.ComponentMenuTags)]
    public class PlayAudioOnClick : MonoBehaviour
    {
        [Tooltip("Target AudioSource. Make sure it's not set to play on awake.")]
        public AudioSource target;
        [Tooltip(("Target clip. If empty, target's clip will be used."))]
        public AudioClip clip;
        [Tooltip("When enabled, the sound is turned on/off on click while it is playing.")]
        public bool toggleOnClick = false;
    }
}
