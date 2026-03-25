#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AsakuShop.Core.Editor
{
    [CustomEditor(typeof(Dialogue))]
    public class DialogueEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Dialogue Lines", EditorStyles.boldLabel);

            SerializedProperty linesProperty = serializedObject.FindProperty("lines");

            for (int i = 0; i < linesProperty.arraySize; i++)
            {
                SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);
                SerializedProperty textProperty = lineProperty.FindPropertyRelative("Text");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.PropertyField(textProperty, GUIContent.none, GUILayout.ExpandWidth(true));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button($"Remove Line {i + 1}", GUILayout.Width(150)))
                {
                    linesProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUILayout.Space(6);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Add New Line"))
            {
                linesProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
