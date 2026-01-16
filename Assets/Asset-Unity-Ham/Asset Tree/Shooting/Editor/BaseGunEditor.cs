using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(BaseGun))]
public class BaseGunEditor : Editor
{
    private List<Type> moduleTypes = new List<Type>();
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle moduleBoxStyle;
    private GUIStyle addButtonStyle;
    private GUIStyle deleteButtonStyle;
    
    private void OnEnable()
    {
        RefreshModuleTypes();
    }

    private void RefreshModuleTypes()
    {
        // Use TypeCache for better performance in modern Unity versions (2019.2+)
        moduleTypes = TypeCache.GetTypesDerivedFrom<GunModule>()
            .Where(t => !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();
    }

    public override void OnInspectorGUI()
    {
        SetupStyles();
        
        serializedObject.Update();

        // Draw the BaseGun's own properties
        DrawHeader();
        
        // Draw the default properties except for the ones we handle specially (if any)
        // We'll just draw the default ones for now
        DrawDefaultGunProperties();

        EditorGUILayout.Space(15);
        
        // Modules Section
        DrawModulesSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        Rect headerRect = EditorGUILayout.GetControlRect(false, 40);
        EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f));
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 18;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        
        EditorGUI.LabelField(headerRect, "BASE GUN SYSTEM", titleStyle);
        EditorGUILayout.Space(5);
    }

    private void DrawDefaultGunProperties()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("CORE SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        SerializedProperty bulletProp = serializedObject.FindProperty("bulletPrefab");
        SerializedProperty spawnProp = serializedObject.FindProperty("spawnPoint");
        SerializedProperty speedProp = serializedObject.FindProperty("bulletSpeed");
        
        EditorGUILayout.PropertyField(bulletProp);
        EditorGUILayout.PropertyField(speedProp);

        // --- Spawn Point Section ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(spawnProp);
        
        BaseGun gun = (BaseGun)target;
        
        if (gun.GetSpawnPoint() == null)
        {
            GUI.color = Color.green;
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                CreateNewSpawnPoint(gun);
            }
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                DeleteSpawnPoint(gun);
            }
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        // Inline Transform editing for SpawnPoint
        if (gun.GetSpawnPoint() != null)
        {
            DrawSpawnPointControls(gun.GetSpawnPoint());
        }
        
        EditorGUILayout.EndVertical();
    }

    private void CreateNewSpawnPoint(BaseGun gun)
    {
        GameObject newSpawn = new GameObject("Spawn Point");
        newSpawn.transform.SetParent(gun.transform);
        newSpawn.transform.localPosition = new Vector3(0, 0, 1);
        newSpawn.transform.localRotation = Quaternion.identity;
        
        Undo.RegisterCreatedObjectUndo(newSpawn, "Create Spawn Point");
        
        SerializedProperty spawnProp = serializedObject.FindProperty("spawnPoint");
        spawnProp.objectReferenceValue = newSpawn.transform;
        serializedObject.ApplyModifiedProperties();
    }

    private void DeleteSpawnPoint(BaseGun gun)
    {
        Transform spawn = gun.GetSpawnPoint();
        if (spawn != null && spawn.parent == gun.transform)
        {
            if (EditorUtility.DisplayDialog("Delete Spawn Point?", "Do you want to delete the Spawn Point GameObject?", "Delete", "Cancel"))
            {
                Undo.DestroyObjectImmediate(spawn.gameObject);
                SerializedProperty spawnProp = serializedObject.FindProperty("spawnPoint");
                spawnProp.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            }
        }
        else
        {
            // Just clear the reference if it's not a direct child
            SerializedProperty spawnProp = serializedObject.FindProperty("spawnPoint");
            spawnProp.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawSpawnPointControls(Transform spawn)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Spawn Point Transform (Local)", EditorStyles.miniBoldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        Vector3 pos = EditorGUILayout.Vector3Field("Position", spawn.localPosition);
        Vector3 rot = EditorGUILayout.Vector3Field("Rotation", spawn.localEulerAngles);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spawn, "Move Spawn Point");
            spawn.localPosition = pos;
            spawn.localEulerAngles = rot;
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawModulesSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("MODULES", headerStyle);
        
        if (GUILayout.Button("Add Module", addButtonStyle, GUILayout.Width(100), GUILayout.Height(25)))
        {
            ShowModuleDropdown();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);

        BaseGun gun = (BaseGun)target;
        GunModule[] currentModules = gun.GetComponents<GunModule>();

        if (currentModules.Length == 0)
        {
            EditorGUILayout.HelpBox("No gun modules attached. Add modules to customize shooting behavior.", MessageType.Info);
        }

        for (int i = 0; i < currentModules.Length; i++)
        {
            DrawModuleInspector(currentModules[i], i);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawModuleInspector(GunModule module, int index)
    {
        if (module == null) return;

        EditorGUILayout.BeginVertical(moduleBoxStyle);
        
        EditorGUILayout.BeginHorizontal();
        
        // Icon
        Rect iconRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(20));
        GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("SettingsIcon").image);
        
        EditorGUILayout.LabelField(module.GetType().Name, EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();

        // Reordering buttons
        if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(20)))
        {
            MoveModule(module, -1);
            return;
        }
        if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(20)))
        {
            MoveModule(module, 1);
            return;
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("✕", deleteButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
        {
            Undo.DestroyObjectImmediate(module);
            GUIUtility.ExitGUI();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        
        // Use CreateEditor to draw the component's inspector
        Editor moduleEditor = Editor.CreateEditor(module);
        
        EditorGUI.BeginChangeCheck();
        moduleEditor.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(module);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void MoveModule(GunModule module, int direction)
    {
        // Unity components reordering is done via UnityEditorInternal.ComponentUtility
        if (direction < 0)
            UnityEditorInternal.ComponentUtility.MoveComponentUp(module);
        else
            UnityEditorInternal.ComponentUtility.MoveComponentDown(module);
    }

    private void ShowModuleDropdown()
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var type in moduleTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => AddModule(type));
        }
        
        if (moduleTypes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No GunModules found in project"));
        }
        
        menu.ShowAsContext();
    }

    private void AddModule(Type type)
    {
        BaseGun gun = (BaseGun)target;
        Undo.AddComponent(gun.gameObject, type);
        EditorUtility.SetDirty(gun);
    }

    private void SetupStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(15, 15, 15, 15);
            boxStyle.margin = new RectOffset(5, 5, 5, 5);
        }

        if (moduleBoxStyle == null)
        {
            moduleBoxStyle = new GUIStyle(EditorStyles.helpBox);
            moduleBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            moduleBoxStyle.margin = new RectOffset(2, 2, 5, 5);
        }

        if (addButtonStyle == null)
        {
            addButtonStyle = new GUIStyle(GUI.skin.button);
            addButtonStyle.fontStyle = FontStyle.Bold;
            addButtonStyle.normal.textColor = Color.white;
        }

        if (deleteButtonStyle == null)
        {
            deleteButtonStyle = new GUIStyle(GUI.skin.button);
            deleteButtonStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            deleteButtonStyle.fontStyle = FontStyle.Bold;
            deleteButtonStyle.alignment = TextAnchor.MiddleCenter;
            deleteButtonStyle.padding = new RectOffset(0, 0, 0, 0);
        }
    }
}

