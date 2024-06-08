using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Needle
{
    public class BetterCubemapEditor : ShaderGUI
    {
        private static readonly int AutoBakeOnChanges = Shader.PropertyToID("_AutoBakeOnChanges");

        private static bool BakedTextureFixIsRunning = false;
        
        [InitializeOnLoadMethod]
        private static void RegisterForLightmapChanges()
        {
            Lightmapping.bakeCompleted += () =>
            {
                BakedTextureFixIsRunning = false;

                var shader = Shader.Find("Skybox/Better Cubemap (Needle)");
                if (!RenderSettings.skybox || RenderSettings.skybox.shader != shader)
                    return;
                
                var mat = RenderSettings.skybox;
                if (mat.GetInt(AutoBakeOnChanges) == 0)
                    return;

                var cubemap = ReflectionProbe.defaultTexture;
                
                if (cubemap)
                {
                    // Debug.Log("Found cubemap at " + locationOfReflProbe0, cubemap);
                }
                else
                {
                    // Debug.LogError("No cubemap after lighting bake! Unity bug? Baking again...");
                    if (!Lightmapping.isRunning) {
                        if (Lightmapping.BakeAsync()) {
                            BakedTextureFixIsRunning = true;
                        }
                    }
                }
                
                if (cubemap)
                    Shader.SetGlobalTexture("_Needle_SkyboxPreconvolutedTex", cubemap);
                
                if (cubemap)
                {
                    mat.SetFloat("_CUBEMAP_USAGE", 1);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_BUILTIN"), false);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_EXTRA"), true);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_ORIGINAL"), false);
                }
                else
                {
                    mat.SetFloat("_CUBEMAP_USAGE", 2);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_BUILTIN"), false);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_EXTRA"), false);
                    mat.SetKeyword(new LocalKeyword(shader, "_CUBEMAP_USAGE_ORIGINAL"), true);
                }

                if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_Tex"))
                    lastCubemap = RenderSettings.skybox.GetTexture("_Tex");
            };
            
            SceneManager.activeSceneChanged += (scene1, scene2) =>
            {
                if (Application.isPlaying) return;
                if (!RenderSettings.skybox) return;
                var shader = RenderSettings.skybox.shader;
                if (!shader || shader.name != "Skybox/Better Cubemap (Needle)") return;
                var mat = RenderSettings.skybox;
                if (mat.GetInt(AutoBakeOnChanges) == 0) return;
                if (!CanAutoBake) return;
                Lightmapping.BakeAsync();
            };
            
            EditorApplication.update += ValidateSkybox;
            
            if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_Tex"))
                lastCubemap = RenderSettings.skybox.GetTexture("_Tex");
            else
                lastCubemap = null;
        }

        private static long lastValidationFrame = -1;
        private static Texture lastCubemap = null;
        internal static void ValidateSkybox()
        {
            if (Time.renderedFrameCount <= lastValidationFrame) return;
            lastValidationFrame = Time.renderedFrameCount;
            
            // we want to rebake lighting if the current selected skybox material
            // doesn't have the correct data set
            if (Lightmapping.isRunning) return;
            if (!RenderSettings.skybox) return;
            var shader = RenderSettings.skybox.shader;
            if (!shader || shader.name != "Skybox/Better Cubemap (Needle)") return;
            
            var mat = RenderSettings.skybox;
            if (mat.GetInt(AutoBakeOnChanges) == 0) return;
            if (!CanAutoBake) return;
            var convolutedCubemap = Shader.GetGlobalTexture("_Needle_SkyboxPreconvolutedTex");
            // theoretically, convolutedCubemap should always be the same as ReflectionProbe.defaultTexture
            var cubemap = mat.GetTexture("_Tex");
            if (!convolutedCubemap || lastCubemap != cubemap)
            {
                lastCubemap = cubemap;
                Lightmapping.BakeAsync();
            }
        }
        
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (!CanAutoBake) return;
            if (Lightmapping.isRunning) return;
                
            if (IsUsedByActiveScene(material))
                Lightmapping.BakeAsync();
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.serializedObject.Update();

            // filter out properties:
            // _Blurriness
            // _LodExposure
            // _BakeBlurriness
            var blurriness = FindProperty("_BackgroundBlurriness", properties);
            var lodExposure = FindProperty("_BackgroundIntensity", properties);
            var bakeBlurriness = FindProperty("_BakeBlurriness", properties);
            var blurToggle = FindProperty("_Lod", properties);
            var filteredProperties = new MaterialProperty[] { blurriness, lodExposure, bakeBlurriness };
            var propertiesWithoutBake = new MaterialProperty[] { blurriness, lodExposure };
            var filtered = properties
                .Except(filteredProperties)
                .Except(new MaterialProperty[] { blurToggle })
                .ToArray();

            materialEditor.SetDefaultGUIWidths();

            var needsBake = false;

            EditorGUI.BeginChangeCheck();
            for (int index = 0; index < filtered.Length; ++index)
            {
                var prop = filtered[index];
                if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
                    materialEditor.ShaderProperty(
                        EditorGUILayout.GetControlRect(true, materialEditor.GetPropertyHeight(prop, prop.displayName),
                            EditorStyles.layerMaskField), prop, prop.displayName);
            }

            needsBake |= EditorGUI.EndChangeCheck();

            var mat = materialEditor.target as Material;
            if (!mat) return;

            var canBake = CanAutoBake;
            EditorGUI.BeginDisabledGroup(!canBake);
            var state = mat.GetInt(AutoBakeOnChanges);
            EditorGUI.BeginChangeCheck();
            state = EditorGUILayout.Toggle(
                new GUIContent("Update Lighting on Changes",
                    "When enabled, lighting is regenerated upon changes. This is the same as clicking \"Generate\" in the Lighting Settings."),
                state == 1)
                ? 1
                : 0;
            if (EditorGUI.EndChangeCheck())
            {
                mat.SetInt(AutoBakeOnChanges, state);
                if (state > 0) needsBake = true;
            }
            EditorGUI.EndDisabledGroup();
            if (!canBake)
            {
                EditorGUILayout.HelpBox("Can't automatically generate lighting: Baked GI or Realtime GI are enabled. Please generate manually.", MessageType.None);
                EditorGUI.BeginDisabledGroup(Lightmapping.isRunning);
                if (GUILayout.Button("Generate Lighting"))
                {
                    Lightmapping.BakeAsync();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
            }

            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(blurToggle, blurToggle.displayName);
            if (EditorGUI.EndChangeCheck() && bakeBlurriness.floatValue > 0)
                needsBake = true;

            if (mat.IsKeywordEnabled("_LOD_ON"))
            {
                EditorGUI.indentLevel++;
                for (int index = 0; index < filteredProperties.Length; ++index)
                {
                    var prop = filteredProperties[index];
                    if (prop == blurriness)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Background Settings", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("These settings affect only the background, not reflections.", EditorStyles.miniLabel);
                    }
                    else if (prop == bakeBlurriness)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Baking Settings", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("These settings affect both reflections and the background.", EditorStyles.miniLabel);
                    }

                    if (!propertiesWithoutBake.Contains(prop))
                        EditorGUI.BeginChangeCheck();
                    if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
                        materialEditor.ShaderProperty(
                            EditorGUILayout.GetControlRect(true, materialEditor.GetPropertyHeight(prop, prop.displayName),
                                EditorStyles.layerMaskField), prop, prop.displayName);
                    if (!propertiesWithoutBake.Contains(prop))
                        needsBake |= EditorGUI.EndChangeCheck();
                }

                EditorGUI.indentLevel--;
            }

            if (BakedTextureFixIsRunning || Lightmapping.isRunning)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Baking lighting...", MessageType.Info);
            }

            // No need to draw the Queue...
            // var emptyArray = new MaterialProperty[] {};
            // materialEditor.PropertiesDefaultGUI(emptyArray);

            if (needsBake && state > 0)
            {
                // check if we can safely kick off a lighting build
                // We can only do that if
                // - auto generate is off or we're in 2023.2+
                // - light mapping is disabled
                // - realtime light mapping is disabled
                if (canBake && mat == RenderSettings.skybox)
                {
                    // check if this is a mouse up
                    var e = Event.current;
                    if (e.type == EventType.MouseUp)
                        Lightmapping.BakeAsync();
                    else if (!Lightmapping.isRunning)
                        Lightmapping.BakeAsync();
                }
                else
                {
                    // TODO set some internal flag so the skybox knows it needs to be baked
                }
            }

            if (CanAutoBake && mat != RenderSettings.skybox)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This material isn't the active skybox and lighting won't be automatically updated. Once you assign this material as skybox, you may need to manually generate lighting.", MessageType.Info);
            }
        }

        private static bool CanAutoBake
        {
            get
            {
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid()) return false; 
                var settings = Lightmapping.GetLightingSettingsForScene(scene);
                if (!settings) return true;
                
                return (settings.bakedGI == false &&
                        settings.realtimeGI == false
    #if !UNITY_2023_2_OR_NEWER
                        && settings.autoGenerate == false
    #endif
                );
            }
        }

        private static bool IsUsedByActiveScene(Material skyboxMaterial)
        {
            /*
            // check if we have the right shader assigned as skybox in any open scene
#if UNITY_2022_2_OR_NEWER
            var scenes = SceneManager.loadedSceneCount;
#else
            var scenes = EditorSceneManager.loadedSceneCount;
#endif
            for (int i = 0; i < scenes; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                // get lighting settings for scene
                var settings = Lightmapping.GetLightingSettingsForScene(scene);
                // var skybox = settings.lightmapper.
                RenderS
            }
            */
            
            return RenderSettings.skybox == skyboxMaterial;
        }
    }   
    
    // This processor strips away _NEEDLE_EDITOR_ON keywords from shaders.
    // For example, our blurred skybox shader has some slow code paths for editor-only blurring,
    // that should never end up in a build.
    internal class ShaderPreprocessor : IPreprocessShaders
    {
        public int callbackOrder => 3;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            var kw = new ShaderKeyword("_NEEDLE_EDITOR_ON");
            for (var i = data.Count - 1; i >= 0; --i)
            {
                if (!data[i].shaderKeywordSet.IsEnabled(kw)) continue;
                data.RemoveAt(i);
                // Debug.Log("Stripped shader variant containing keyword " + kw + ", now: " + string.Join(" ", data[i].shaderKeywordSet.GetShaderKeywords().Select(x => x.name)));
            }
        }
    }
}