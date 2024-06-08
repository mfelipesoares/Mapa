#if HAVE_NEEDLE_INSPECTOR

using Needle.Engine;
using Needle.Engine.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editor
{
    [ToolbarElement(AutoInject = false)]
    internal class BuildButton : IToolbarElement
    {
        [InitializeOnLoadMethod]
        static void Inject()
        {
            // TODO add to center
            new BuildButton().Add(ToolbarSide.Right);    
        }

        // ??
        public VisualElement CreateVisualElement()
        {
            return new ToolbarButton(BuildNow) { text = "Build" };
        }

        private Texture needleIcon;
        private GUIContent btnContent;
        private GUIStyle style;
        public void OnGUI(Rect rect)
        {
            if (!needleIcon)
                needleIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f"));
            if (btnContent == null)
                btnContent = new GUIContent("Build", needleIcon, "Build Dev");
            if (style == null)
            {
                style = new GUIStyle("AppCommand");
                const int padding = 3;
                style.padding = new RectOffset(padding, padding, padding, padding);
            }
            
            if (GUI.Button(rect, btnContent, style))
                BuildNow();
        }

        private async void BuildNow()
        {
            await Builder.Build(false, BuildContext.Development);
        }

        public bool Visible { get => true; set {} }
        public float Width { get => 40; set {} }
    }
}

#endif