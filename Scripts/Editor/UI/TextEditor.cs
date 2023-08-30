using Slimple.Core;
using Slimple.UI;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UI;
using UnityEngine;

namespace Slimple.Editor.UI
{
    [CustomEditor(typeof(Slimple.UI.Text), true)]
    [CanEditMultipleObjects]
    public class TextEditor : GraphicEditor
    {
        private SerializedProperty m_Text;
        private SerializedProperty m_TextPropertyData;
        private SerializedProperty m_Font;
        private SerializedProperty m_FontStyle;
        private SerializedProperty m_FontSize;
        private SerializedProperty m_Direction;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_HorizontalOverflow;
        private SerializedProperty m_VerticalOverflow;
        
        private GUIContent m_LeftAlignText;
        private GUIContent m_CenterAlignText;
        private GUIContent m_RightAlignText;
        private GUIContent m_LeftAlignTextActive;
        private GUIContent m_CenterAlignTextActive;
        private GUIContent m_RightAlignTextActive;
        private GUIContent m_UpperAlignText;
        private GUIContent m_MiddleAlignText;
        private GUIContent m_LowerAlignText;
        private GUIContent m_UpperAlignTextActive;
        private GUIContent m_MiddleAlignTextActive;
        private GUIContent m_LowerAlignTextActive;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            m_Text = serializedObject.FindProperty(nameof(m_Text));
            
            m_TextPropertyData = serializedObject.FindProperty(nameof(m_TextPropertyData));
            m_Font = m_TextPropertyData.FindPropertyRelative(nameof(m_Font));
            m_FontStyle = m_TextPropertyData.FindPropertyRelative(nameof(m_FontStyle));
            m_FontSize = m_TextPropertyData.FindPropertyRelative(nameof(m_FontSize));
            m_Alignment = m_TextPropertyData.FindPropertyRelative(nameof(m_Alignment));
            m_HorizontalOverflow = m_TextPropertyData.FindPropertyRelative(nameof(m_HorizontalOverflow));
            m_VerticalOverflow = m_TextPropertyData.FindPropertyRelative(nameof(m_VerticalOverflow));
            m_Direction = m_TextPropertyData.FindPropertyRelative(nameof(m_Direction));

            // Horizontal Alignment Icons
            m_LeftAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_left", "Left Align");
            m_CenterAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_center", "Center Align");
            m_RightAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_right", "Right Align");
            m_LeftAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_left_active", "Left Align");
            m_CenterAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_center_active", "Center Align");
            m_RightAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_right_active", "Right Align");

            // Vertical Alignment Icons
            m_UpperAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_top", "Top Align");
            m_MiddleAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_center", "Middle Align");
            m_LowerAlignText = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_bottom", "Bottom Align");
            m_UpperAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_top_active", "Top Align");
            m_MiddleAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_center_active", "Middle Align");
            m_LowerAlignTextActive = EditorGUIUtility.IconContent(@"GUISystem/align_vertically_bottom_active", "Bottom Align");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_Text);
            
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.ObjectField(m_Font, typeof(Font));
                EditorGUILayout.PropertyField(m_FontStyle);
                EditorGUILayout.PropertyField(m_FontSize);
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("Paragraph", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                {
                    EditorGUIUtility.SetIconSize(new Vector2(15, 15));
                    GUILayout.BeginHorizontal();
                    
                    EditorGUILayout.PrefixLabel(EditorGUIUtility.TrTextContent("Alignment"));
                    GUILayout.Space(1.5f);
                    
                    bool left = false, center = false, right = false, upper = false, middle = false, lower = false;
                    foreach (var @object in m_Alignment.serializedObject.targetObjects)
                    {
                        Text textComponent = @object as Text;
                        var alignment = textComponent.alignment;
                        left = left ||
                               alignment == TextAnchor.LowerLeft ||
                               alignment == TextAnchor.MiddleLeft ||
                               alignment == TextAnchor.UpperLeft;
                        center = center ||
                               alignment == TextAnchor.LowerCenter ||
                               alignment == TextAnchor.MiddleCenter ||
                               alignment == TextAnchor.UpperCenter;
                        right = right ||
                               alignment == TextAnchor.LowerRight ||
                               alignment == TextAnchor.MiddleRight ||
                               alignment == TextAnchor.UpperRight;
                        upper = upper ||
                               alignment == TextAnchor.UpperLeft ||
                               alignment == TextAnchor.UpperCenter ||
                               alignment == TextAnchor.UpperRight;
                        middle = middle ||
                               alignment == TextAnchor.MiddleLeft ||
                               alignment == TextAnchor.MiddleCenter ||
                               alignment == TextAnchor.MiddleRight;
                        lower = lower ||
                                alignment == TextAnchor.LowerLeft ||
                                alignment == TextAnchor.LowerCenter ||
                                alignment == TextAnchor.LowerRight;
                    }
                    bool left_ = left, center_ = center, right_ = right, upper_ = upper, middle_ = middle, lower_ = lower;
                    left   = left   != GUILayout.Toggle(left,   left   ? m_LeftAlignText   : m_LeftAlignTextActive,   EditorStyles.miniButtonLeft,  GUILayout.Width(20));
                    center = center != GUILayout.Toggle(center, center ? m_CenterAlignText : m_CenterAlignTextActive, EditorStyles.miniButtonMid,   GUILayout.Width(20));
                    right  = right  != GUILayout.Toggle(right,  right  ? m_RightAlignText  : m_RightAlignTextActive,  EditorStyles.miniButtonRight, GUILayout.Width(20));
                    upper  = upper  != GUILayout.Toggle(upper,  upper  ? m_UpperAlignText  : m_UpperAlignTextActive,  EditorStyles.miniButtonLeft,  GUILayout.Width(20));
                    middle = middle != GUILayout.Toggle(middle, middle ? m_MiddleAlignText : m_MiddleAlignTextActive, EditorStyles.miniButtonMid,   GUILayout.Width(20));
                    lower  = lower  != GUILayout.Toggle(lower,  lower  ? m_LowerAlignText  : m_LowerAlignTextActive,  EditorStyles.miniButtonRight, GUILayout.Width(20));
                    if (!left  && !center && !right) { left  = left_;  center = center_; right = right_; }
                    if (!upper && !middle && !lower) { upper = upper_; middle = middle_; lower = lower_; }
                    if (upper  && left)   m_Alignment.enumValueIndex = (int)TextAnchor.UpperLeft;
                    if (upper  && center) m_Alignment.enumValueIndex = (int)TextAnchor.UpperCenter;
                    if (upper  && right)  m_Alignment.enumValueIndex = (int)TextAnchor.UpperRight;
                    if (middle && left)   m_Alignment.enumValueIndex = (int)TextAnchor.MiddleLeft;
                    if (middle && center) m_Alignment.enumValueIndex = (int)TextAnchor.MiddleCenter;
                    if (middle && right)  m_Alignment.enumValueIndex = (int)TextAnchor.MiddleRight;
                    if (lower  && left)   m_Alignment.enumValueIndex = (int)TextAnchor.LowerLeft;
                    if (lower  && center) m_Alignment.enumValueIndex = (int)TextAnchor.LowerCenter;
                    if (lower  && right)  m_Alignment.enumValueIndex = (int)TextAnchor.LowerRight;
                    
                    GUILayout.EndHorizontal();
                    EditorGUIUtility.SetIconSize(Vector2.zero);
                }
                
                EditorGUILayout.PropertyField(m_HorizontalOverflow);
                EditorGUILayout.PropertyField(m_VerticalOverflow);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_Direction);
            }
            EditorGUI.indentLevel--;

            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
