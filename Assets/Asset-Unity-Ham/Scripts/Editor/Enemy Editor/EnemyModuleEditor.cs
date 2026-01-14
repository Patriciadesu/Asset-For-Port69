using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for all EnemyModule derived classes.
/// Provides enhanced UI for module management.
/// Place this file in an "Editor" folder in your project.
/// </summary>
[CustomEditor(typeof(EnemyModule), true)]
public class EnemyModuleEditor : Editor
{
    private SerializedProperty isActiveProp;
    private EnemyModule module;

    private void OnEnable()
    {
        module = (EnemyModule)target;
        isActiveProp = serializedObject.FindProperty("IsActive");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Module header
        EditorGUILayout.Space(5);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        string moduleName = target.GetType().Name;
        EditorGUILayout.LabelField($"{moduleName}", headerStyle);
        EditorGUILayout.Space(5);

        // Active toggle with prominent styling
        EditorGUILayout.BeginVertical("box");

        GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = module.IsActive ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(isActiveProp, new GUIContent("Module Active"));

        string statusText = module.IsActive ? "ENABLED" : "DISABLED";
        Color statusColor = module.IsActive ? Color.green : Color.red;

        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.normal.textColor = statusColor;
        statusStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Draw default inspector for remaining properties
        DrawPropertiesExcluding(serializedObject, "m_Script", "IsActive");

        // Module info
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Module Information", EditorStyles.boldLabel);

        // Get description from tooltip if available
        var moduleType = target.GetType();
        var descriptionAttr = (TooltipAttribute)System.Attribute.GetCustomAttribute(
            moduleType, typeof(TooltipAttribute));

        if (descriptionAttr != null)
        {
            EditorGUILayout.LabelField("Description:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(descriptionAttr.tooltip, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.LabelField($"Type: {moduleName}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        // Quick actions
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(module.IsActive ? "Disable Module" : "Enable Module", GUILayout.Height(25)))
            {
                module.IsActive = !module.IsActive;
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}