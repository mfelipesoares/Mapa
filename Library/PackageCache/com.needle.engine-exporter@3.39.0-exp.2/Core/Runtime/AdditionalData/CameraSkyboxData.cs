using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace Needle.Engine.AdditionalData
{
    public class CameraSkyboxData : AdditionalComponentData<Camera>
    {
        internal const string UnitySkyboxShaderName = "Skybox/Cubemap";
        internal const string SkyboxShaderName = "Skybox/Better Cubemap (Needle)";
        
        [Range(0, 1)]
        public float backgroundBlurriness = 0;
        [Range(0, 2)]
        public float backgroundIntensity = 1;

        public static readonly int BackgroundIntensity = Shader.PropertyToID("_BackgroundIntensity");
        public static readonly int BackgroundBlurriness = Shader.PropertyToID("_BackgroundBlurriness");

#if UNITY_EDITOR
        
        // auto-upgrade is disabled while component is still experimental
        /*
        private void OnValidate()
        {
            // check if we can upgrade from Skybox/Cubemap to Skybox/Better Cubemap (Needle)
            if (!RenderSettings.skybox) return;
            var mat = RenderSettings.skybox;

            // Only shader we can upgrade right now
            if (mat.shader.name != UnitySkyboxShaderName)
            {
                UpgradeSkyboxMaterial();
            }
        }
        */

        internal void UpgradeSkyboxMaterial()
        {
            var mat = RenderSettings.skybox;
            if (!mat) return;
            Undo.RegisterCompleteObjectUndo(mat, "Changed Skybox Shader to " + SkyboxShaderName);
            var materialEditor = Editor.CreateEditor(mat, null) as MaterialEditor;
            var shader = Shader.Find(SkyboxShaderName);
            if (!shader) return;
                    
            if (materialEditor)
                materialEditor.SetShader(shader);
            else 
                mat.shader = shader;
                
            mat.SetFloat(BackgroundBlurriness, backgroundBlurriness);
            mat.SetFloat(BackgroundIntensity, backgroundIntensity);
            EditorUtility.SetDirty(mat);
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(CameraSkyboxData))]
    internal class CameraSkyboxDataEditor : Editor
    {
        private void OnEnable()
        {
            // sync values from material to data
            var mat = RenderSettings.skybox;
            var t = (CameraSkyboxData) target;
            if (!t) return;
            if (mat.shader.name == CameraSkyboxData.SkyboxShaderName)
            {
                t.backgroundBlurriness = mat.GetFloat(CameraSkyboxData.BackgroundBlurriness);
                t.backgroundIntensity = mat.GetFloat(CameraSkyboxData.BackgroundIntensity);
            }
        }

        public override void OnInspectorGUI()
        {
            var mat = RenderSettings.skybox;
            if (DrawDefaultInspector())
            {
                var t = (CameraSkyboxData) target;
                if (!t) return;
                if (mat.shader.name == CameraSkyboxData.SkyboxShaderName)
                {
                    mat.SetFloat(CameraSkyboxData.BackgroundBlurriness, t.backgroundBlurriness);
                    mat.SetFloat(CameraSkyboxData.BackgroundIntensity, t.backgroundIntensity);
                    EditorUtility.SetDirty(mat);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
            var newMat = EditorGUILayout.ObjectField("Skybox Material", mat, typeof(Material), false) as Material;
            if (newMat != mat)
            {
                if (_getRenderSettingsMethod == null)
                    _getRenderSettingsMethod = typeof(RenderSettings).GetMethod("GetRenderSettings", BindingFlags.Static | BindingFlags.NonPublic);
                if (_getRenderSettingsMethod == null) return;
                Undo.RecordObject(_getRenderSettingsMethod.Invoke(null, null) as RenderSettings, "Assign Skybox Material");
                RenderSettings.skybox = newMat;
            }
            
            if (mat && mat.shader && mat.shader.name == CameraSkyboxData.UnitySkyboxShaderName)
            {
                EditorGUILayout.Space();
                
                EditorGUILayout.HelpBox("To see a preview of background blurriness and intensity, you can upgrade the current skybox to a Needle Skybox with more options.", MessageType.Info);
                if (GUILayout.Button("Upgrade now"))
                {
                    var t = (CameraSkyboxData) target;
                    t.UpgradeSkyboxMaterial();
                }
            }
        }
        
        private static MethodInfo _getRenderSettingsMethod;
    }
#endif
}
