using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
    [CustomPropertyDrawer(typeof(ValueIfComponentDoesntExist))]
    public class ValueIfComponentDoesntExistDrawer : PropertyDrawer
    {
        /** We could do this nicer if we
         *  would use and adjust the outputs of the internal:
         *    ScriptAttributeUtility.GetHandler(property).OnGUI(position, property, label, includeChildren);
         *  because then we might even be able to use the same tooltip as on the target property.
         */
        
        private static GUIStyle paneOptions;
        
        private Object lastTarget;
        private Object cachedComponent;
        private SerializedObject cachedObject;
        private SerializedProperty cachedProperty;
        private string targetPropertyTooltip;
        private string error;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUI.GetPropertyHeight(property);
            if (cachedProperty != null) height = EditorGUI.GetPropertyHeight(cachedProperty);
            if (error != null) height += EditorGUIUtility.singleLineHeight * 1;
            return height;
        }

        private void CheckForHierarchyChanges()
        {
            lastTarget = null;
            cachedObject = null;
            cachedProperty = null;
            cachedComponent = null;
            foreach(var e in ActiveEditorTracker.sharedTracker.activeEditors)
                e.Repaint();
        }

        private void DrawExtraMenu(Rect position, Action buttonClicked)
        {
            // ensure bottom-aligned
            position.y = position.yMax - EditorGUIUtility.singleLineHeight;
            position.height = EditorGUIUtility.singleLineHeight;
            
            if (paneOptions == null)
            {
                paneOptions = new GUIStyle("PaneOptions");
                var buttonStyle = (GUIStyle) "button";
                var margin = new RectOffset(buttonStyle.margin.left, buttonStyle.margin.right, buttonStyle.margin.top, buttonStyle.margin.bottom);
                margin.top = 3;
                margin.left = 0;
                margin.right = 2;
                paneOptions.margin = margin;
            }

            var c = GUI.color;
            var c2 = c;
            c2.a = 0.5f;
            GUI.color = c2;
            var tooltip = cachedComponent ? "This property is controlled by another component. Click to show more options." : "";
            if (GUI.Button(position, new GUIContent("", tooltip), paneOptions))
                buttonClicked();
            GUI.color = c;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var t = attribute as ValueIfComponentDoesntExist;
            var target = property.serializedObject.targetObject as Component;
            if (!target || t == null) return;
            
            if (target != lastTarget)
            {
                // Potentially better: use ActiveEditorTracker.sharedTracker.activeEditors
                EditorApplication.hierarchyChanged -= CheckForHierarchyChanges;
                EditorApplication.hierarchyChanged += CheckForHierarchyChanges;

                if (!string.IsNullOrEmpty(t.propertyName))
                {
                    var fieldName = t.propertyName;
                    if (fieldName == "m_Enabled") fieldName = "enabled";
                    var field = t.type.GetMember(fieldName, (BindingFlags)(-1));
                    if (field.Length < 1)
                    {
                        // search in parent types
                        var parentType = t.type;
                        while (field.Length < 1 && parentType.BaseType != null)
                        {
                            parentType = parentType.BaseType;
                            field = parentType.GetMember(fieldName, (BindingFlags)(-1));
                        }
                        if (field.Length < 1)
                        {
                            error = $"Field {t.propertyName} not found in {t.type}";
                        }
                    }

                    if (field.Length > 0)
                    {
                        // for showing the delegated property's tooltip on the original property
                        targetPropertyTooltip = field[0].GetCustomAttribute<TooltipAttribute>()?.tooltip;
                    }
                }

                cachedComponent = target.GetComponent(t.type);
                if (!cachedComponent)
                    cachedComponent = Object.FindAnyObjectByType(t.type, FindObjectsInactive.Exclude);

                if (cachedComponent && t.propertyName != null)
                {
                    cachedObject = new SerializedObject(cachedComponent);
                    cachedProperty = cachedObject.FindProperty(t.propertyName);

                    if (cachedProperty == null)
                    {
                        error = $"SerializedProperty {t.propertyName} not found in {t.type}";
                    }
                }
                
                lastTarget = target;
            }

            if (error != null)
            {
                var errorRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 1);
                EditorGUI.HelpBox(errorRect, error, MessageType.Error);
                position.y += errorRect.height;
            }

            if (!cachedComponent)
            {
                // draw colored quad for debugging
                /*
                var color = GUI.color;
                GUI.color = new Color(1, 0.5f, 0.5f, 0.5f);
                EditorGUI.DrawRect(position, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                GUI.color = color;
                */
                
                // make space for a PaneOptions menu
                if (t.menuMode == ValueIfComponentDoesntExist.MenuMode.AddComponentMenu)
                    position.width -= 20;
                
                // we can directly use the tooltip from the target class and property, unless it's explicitly defined
                if (targetPropertyTooltip != null && string.IsNullOrEmpty(label.tooltip))
                    label.tooltip = targetPropertyTooltip;
                
                if (t.menuMode == ValueIfComponentDoesntExist.MenuMode.AddComponentButton)
                    position.xMax -= 20;
                
                EditorGUI.PropertyField(position, property, label, true);
                
                if (t.menuMode == ValueIfComponentDoesntExist.MenuMode.AddComponentButton)
                {
                    position.x += position.width;
                    position.width = 20;
                    if (GUI.Button(position, new GUIContent("+", $"Add component: {t.type.Name} to see more settings"), EditorStyles.miniButton))
                    {
                        Undo.RegisterCompleteObjectUndo(target.gameObject, $"Add {t.type.Name} to {target.name}");
                        Undo.AddComponent(target.gameObject, t.type);
                        lastTarget = null;
                    }
                }
                else if (t.menuMode == ValueIfComponentDoesntExist.MenuMode.AddComponentMenu)
                {
                    position.x += position.width;
                    position.width = 20;
                    
                    DrawExtraMenu(position, () =>
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent($"Add component: {t.type.Name} to see more settings"), false, () =>
                        {
                            Undo.RegisterCompleteObjectUndo(target.gameObject, $"Add {t.type.Name} to {target.name}");
                            Undo.AddComponent(target.gameObject, t.type);
                            lastTarget = null;
                        });
                        menu.ShowAsContext();
                    });
                }
            }
            else
            {
                /*
                // draw colored quad for debugging
                var color = GUI.color;
                GUI.color = new Color(0, 1f, 0.5f, 0.5f);
                EditorGUI.DrawRect(position, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                GUI.color = color;
                */
                
                // IDEA: draw tiny quad similar to prefab overrides
                /*
                var tinyQuad = position;
                var dotHeight = 2; //EditorGUIUtility.singleLineHeight;
                tinyQuad.width = 2;
                tinyQuad.height = dotHeight;
                tinyQuad.x -= 6;
                tinyQuad.y = position.yMax - EditorGUIUtility.singleLineHeight * 0.5f - dotHeight * 0.5f;
                EditorGUI.DrawRect(tinyQuad, new Color(0, 1f, 0.5f, 0.5f));
                */ 
                
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    position.width -= 20;
                    
                    if (t.propertyName == null)
                    {
                        var content = new GUIContent(label);
                        if (!string.IsNullOrEmpty(t.labelIfComponentExists))
                            content.text = t.labelIfComponentExists;
                        content.tooltip = $"Adjust settings on the {t.type.Name} component or remove it to use the default value.\n\n{content.tooltip}";
                        var position1 = EditorGUI.PrefixLabel(position, content);
                        position.xMin = position1.x;
                        if (GUI.Button(position, "Properties", EditorStyles.miniButton))
                        {
                            PublicEditorGUI.OpenPropertyEditor(cachedComponent);
                        }
                    }
                    else if (cachedProperty != null)
                    {
                        cachedObject.UpdateIfRequiredOrScript();
                        EditorGUI.PropertyField(position, cachedProperty, label);
                        
                        if (check.changed)
                        {
                            cachedObject.ApplyModifiedProperties();
                        }
                    }
                    
                    position.x += position.width;
                    position.width = 20;
                    DrawExtraMenu(position, () =>
                    {
                        var menu = new GenericMenu();
                        menu.AddDisabledItem(new GUIContent("This property is controlled by " + cachedComponent.GetType().Name));
                        menu.AddItem(new GUIContent("Show all properties"), false, () =>
                        {
                            PublicEditorGUI.OpenPropertyEditor(cachedComponent);
                        });
                        menu.AddItem(new GUIContent($"Ping {cachedComponent.GetType().Name} on GameObject \"{cachedComponent.name}\""), false, () => {
                            EditorGUIUtility.PingObject(cachedComponent);
                        });
                        menu.ShowAsContext();
                    });
                }
            }
        }
    }
}
