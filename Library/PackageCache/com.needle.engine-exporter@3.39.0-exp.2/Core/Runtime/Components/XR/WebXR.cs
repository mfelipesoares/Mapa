using System;
using System.Text;
using Needle.Engine.Components;
using Needle.Engine.Components.XR;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
    [AddComponentMenu("Needle Engine/XR/WebXR" + Constants.NeedleComponentTags + " WebXR")]
    public class WebXR : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("When enabled, a button will be added to the UI to enter VR")]
        public bool createVRButton = true;
        [Tooltip("When enabled, a button will be added to the UI to enter AR")]
        public bool createARButton = true;
        [Tooltip("When enabled, a send to quest button will be shown if the device does not support VR")]
        public bool createSendToQuestButton = true;
        [Tooltip("When enabled, a QR code will be shown to open the scene on a mobile device")]
        [ValueIfComponentDoesntExist(typeof(NeedleMenu), nameof(NeedleMenu.CreateQRCodeButton))]
        public bool createQRCode = true;
        
        [Header("AR")]
        [Info("Add WebARSessionRoot to this GameObject for more properties", InfoAttribute.InfoType.None, new []{ typeof(WebARSessionRoot) })]
        [Tooltip("When enabled, the user manually places the scene in AR")]
        [ValueIfComponentDoesntExist(typeof(WebARSessionRoot), null, "Session Root", ValueIfComponentDoesntExist.MenuMode.AddComponentButton)]
        public bool usePlacementReticle = true;
        [ValueIfComponentDoesntExist(typeof(WebARSessionRoot), nameof(WebARSessionRoot.arTouchTransform))]
        public bool usePlacementAdjustment = true;
        [ValueIfComponentDoesntExist(typeof(WebARSessionRoot), nameof(WebARSessionRoot.arScale))]
        public float arSceneScale = 1;
        [ValueIfComponentDoesntExist(typeof(WebARSessionRoot), nameof(WebARSessionRoot.useXRAnchor))]
        public bool useXRAnchor = false;
        [Tooltip("When enabled, a USDZExporter component will be added to the scene (if none is found)")]
        [ValueIfComponentDoesntExist(typeof(USDZExporter), null, "QuickLook Export", ValueIfComponentDoesntExist.MenuMode.AddComponentButton)]
        public bool useQuicklookExport = true;
        [Tooltip("Enables the 'depth-sensing' WebXR feature to provide realtime depth occlusion. Only supported on Oculus Quest right now.")]
        public bool useDepthSensing = false;

        [Header("VR")]
        [Info("Add XRControllerMovement and XRControllerModel components to this GameObject for more properties.\nMove, rotate and teleport using your controller thumbsticks", InfoAttribute.InfoType.None, new []{ typeof(XRControllerMovement), typeof(XRControllerModel) })]
        [Tooltip("When enabled, default movement behaviour will be added (XRControllerMovement component).\nThis allows you to move around in VR using controllers: Use the thumbstick to rotate, teleport or move around.")]
        [ValueIfComponentDoesntExist(typeof(XRControllerMovement), null, "Controls", ValueIfComponentDoesntExist.MenuMode.AddComponentButton)]
        public bool useDefaultControls = true;
        [ValueIfComponentDoesntExist(typeof(XRControllerModel), nameof(XRControllerModel.CreateControllerModel), null, ValueIfComponentDoesntExist.MenuMode.AddComponentButton)]
        public bool showControllerModels = true;
        [ValueIfComponentDoesntExist(typeof(XRControllerModel), nameof(XRControllerModel.CreateHandModel))]
        public bool showHandModels = true;
        
        [Header("Avatar")]
        [Info("This avatar representation will be spawned when you enter a WebXR session")]
        public Transform defaultAvatar;

        private void OnDrawGizmosSelected()
        {
            if (!Object.FindAnyObjectByType<XRRig>())
            {
                var mat = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                XRRig.RenderRigGizmos(mat, true);
            }
        }

        public void enterVR() { }
        public void enterAR() { }
        public void exitXR() { }
        public void setDefaultMovementEnabled(bool enabled) { }
        public void setDefaultControllerRenderingEnabled(bool enabled) { }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebXR))]
        private class WebXREditor : Editor
        {
            private int _paintId = 0;
            private XRRig[] _rigsInScene;

            private void OnEnable()
            {
                _rigsInScene = FindObjectsByType<XRRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var webxr = (WebXR)this.target;
                
                if (_paintId % 10 == 0)
                {
                    _rigsInScene = FindObjectsByType<XRRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                }
                
                DrawAvatarInfo(webxr);
                DrawRigInfo(webxr);
            }

            private void DrawAvatarInfo(WebXR webxr)
            {
                if (webxr.defaultAvatar)
                {
                    var warnings = new StringBuilder();
                    var isAsset = EditorUtility.IsPersistent(webxr.defaultAvatar);
                    
                    var hasAvatarComponent = webxr.defaultAvatar.GetComponent<Components.XR.Avatar>();
                    
                    // if the scene has a synced room component but the avatar has no PlayerState then networking for the avatar wont work
                    var needsPlayerState = 
                        !webxr.defaultAvatar.TryGetComponent<PlayerState>(out _) &&
                        FindAnyObjectByType<SyncedRoom>();
                    
                    if (isAsset == false) warnings.AppendLine("The default avatar should be a Prefab asset");
                    if (needsPlayerState) warnings.AppendLine("The default avatar should have a PlayerState component");
                    if (hasAvatarComponent == false) warnings.AppendLine("The default avatar should have an Avatar component");

                    if (warnings.Length > 0)
                    {
                        EditorGUILayout.HelpBox(warnings.ToString().TrimEnd('\n'), MessageType.Warning);
                        if (GUILayout.Button("Fix Avatar"))
                        {
                            if (!hasAvatarComponent)
                            {
                                Undo.AddComponent<Components.XR.Avatar>(webxr.defaultAvatar.gameObject);
                                Debug.Log("Added Avatar to " + webxr.defaultAvatar.name, webxr.defaultAvatar);
                            }
                            
                            if (needsPlayerState)
                            {
                                Undo.AddComponent<PlayerState>(webxr.defaultAvatar.gameObject);
                                Debug.Log("Added PlayerState to " + webxr.defaultAvatar.name, webxr.defaultAvatar);
                            }

                            if (isAsset == false)
                            {
                                var path = "Assets/" + webxr.defaultAvatar.name + ".prefab";
                                var prefab = PrefabUtility.SaveAsPrefabAsset(webxr.defaultAvatar.gameObject, path);
                                Undo.DestroyObjectImmediate(webxr.defaultAvatar.gameObject);
                                webxr.defaultAvatar = prefab.transform;
                                Debug.Log("Created prefab at " + path, prefab);
                            }
                        }
                    }
                }
            }

            private void DrawRigInfo(WebXR webxr)
            {
                if (webxr.createVRButton)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("XR Rig", EditorStyles.boldLabel);
                    if (_rigsInScene.Length <= 0)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox("Add a XR Rig component to your scene to configure the start position, rotation and size in VR (default is world center)", MessageType.None);
                            if (GUILayout.Button("Add XRRig", GUILayout.Height(30)))
                            {
                                var obj = new GameObject("XR Rig");
                                obj.transform.parent = webxr.transform.parent;
                                obj.transform.SetSiblingIndex(webxr.transform.GetSiblingIndex()+1);
                                obj.transform.position = webxr.transform.position;
                                obj.transform.rotation = webxr.transform.rotation;
                                Undo.RegisterCreatedObjectUndo(obj, "Add XR Rig");
                                Undo.AddComponent<XRRig>(obj);
                                Selection.activeGameObject = obj;
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Found " + _rigsInScene.Length + " XR Rig components in the scene", MessageType.None);
                        foreach (var rig in _rigsInScene)
                        {
                            if (!rig) continue;
                            EditorGUILayout.ObjectField(rig.name + " [" + rig.priority + "]", rig, typeof(XRRig), true);
                        }
                    }
                }
                
            }
        }
        
        #endif
    }
}
