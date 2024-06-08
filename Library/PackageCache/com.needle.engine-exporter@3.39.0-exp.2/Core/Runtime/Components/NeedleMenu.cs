using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu("Needle Engine/Needle Menu" + Needle.Engine.Constants.NeedleComponentTags)]
    public class NeedleMenu : MonoBehaviour
    {
        public enum MenuPosition
        {
            // Top = 0,
            Bottom = 1
        }
        
        [JsonIgnore]
        [Tooltip("When enabled the Needle Engine menu will be at the top of the screen")]
        public MenuPosition _position = MenuPosition.Bottom;
        [UsedImplicitly] public string Position => _position.ToString().ToLowerInvariant();
        
        // [Info("When enabled the Needle Logo will be visible in the Needle Engine menu")]
        [RequireLicense(LicenseType.Indie, null, "Needle Engine License is required to hide the Needle Logo")]
        [Tooltip("When enabled the Needle Logo will be visible in the Needle Engine menu")]
        public bool ShowNeedleLogo = true;

        public bool CreateFullscreenButton = true;
        public bool CreateMuteButton = true;
        [Tooltip("Creates a button to show a QRCode. The QRCode can be scanned to open the website on a mobile device")]
        public bool CreateQRCodeButton = true;

        [Header("Preview")]
        [Tooltip("When enabled the menu will be visible in VR/AR when you look up")]
        public bool ShowSpatialMenu = true;
    }
}