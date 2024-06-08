using System;
using System.CodeDom;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Needle.Engine.Components
{
    [Serializable]
    public class QuicklookOverlay
    {
        public string CheckoutTitle = "🌵 Made with Needle";
        public string CheckoutSubtitle = "";
        public string CallToAction = "Learn More";

        [Header("Optional"),
         Tooltip(
             "Optionally assign an URL to open when the user clicks the call to action button. If none is assigned the button will just close Quicklook")]
        public string CallToActionURL = "https://needle.tools";
    }
    
    [AddComponentMenu("Needle Engine/USDZ Exporter" + Needle.Engine.Constants.NeedleComponentTags + " Quicklook iOS Apple AR")]
    public class USDZExporter : MonoBehaviour
    {
        [Tooltip("Assign a part if your hierarchy to export. If none assigned it will export it's children")]
        public Transform objectToExport;
        public bool allowCreateQuicklookButton = true;
        public bool autoExportAnimations = true;
        [Tooltip("Interactive USDZ files only work on Apple's QuickLook (Augmented Reality on iOS). They use preliminary USD behaviours.\nSome Needle components are automatically translated to USDZ behaviors and there's an API to add custom ones.")]
        public bool interactive = true;

        [FormerlySerializedAs("overlay")] [RequireLicense(LicenseType.Pro, null, "Custom Branding requires a commercial license")]
        public QuicklookOverlay customBranding;
        [RequireLicense(LicenseType.Pro)]
        public string exportFileName = "Needle";
        
        [Tooltip("Leave this field free for dynamic export. Specify a custom .usdz or .reality file. The file should be in the \"assets\" directory in the web project.")]
        #if UNITY_EDITOR
        [FileReferenceType(typeof(DefaultAsset), ".usdz")]
        #endif
        public FileReference customUsdzFile;
        
        internal const string ComponentMenuPrefix = "Needle Engine/Everywhere Actions/";
        internal const string USDZOnlyMenuPrefix = "Needle Engine/QuickLook Actions/";
        internal const string ComponentMenuTags = Constants.NeedleComponentTags + "Everywhere USDZ QuickLook";
        internal const string USDZOnlyMenuTags = Constants.NeedleComponentTags + "USDZ QuickLook";

        // We want the toggle
        // ReSharper disable once Unity.RedundantEventFunction
        private void OnEnable() { }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(USDZExporter))]
    internal class USDZExporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // var t = (USDZExporter)target;
            // if (!t.objectToExport && t.transform.childCount <= 0)
            // {
            //     EditorGUILayout.HelpBox($"No objects to export: Assign a part of your hierarchy to the {nameof(USDZExporter.objectToExport)} field or add children to this object.", MessageType.Warning);
            // }
        }
    }
    #endif
}
