using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(Floor))]
public class FloorEditor : Editor
{
    private string[] objetiveTypes;
    private int selectedIndex;

    private void OnEnable()
    {
        // Find all types inheriting from QuestExtension
        var types = typeof(FloorEffect).Assembly.GetTypes();
        objetiveTypes = types
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(FloorEffect)))
            .Select(t => t.FullName) // Use FullName to avoid namespace issues
            .ToArray();
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Floor quest = (Floor)target;

        #region Add Objectives
        EditorGUILayout.Space(10);
        // Dropdown to select extension type
        selectedIndex = EditorGUILayout.Popup("Add Objective", selectedIndex, objetiveTypes);

        if (GUILayout.Button("Add Objective"))
        {
            AddExtension(quest, objetiveTypes[selectedIndex]);
        }
        #endregion

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(quest);
        }
    }
    private void AddExtension(Floor quest, string objectiveTypeName)
    {
        var type = Type.GetType(objectiveTypeName);
        if (type != null)
        {
            FloorEffect objective = (FloorEffect)Activator.CreateInstance(type);
            objective.name = objectiveTypeName;
            quest.objectives.Add(objective);
        }
        else
        {
            Debug.LogError($"Could not find type: {objectiveTypeName}");
        }
    }
}
