﻿using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    private bool showDoNotTouch = false; // Track foldout state
    private Dictionary<string, bool> extensionToggles = new Dictionary<string, bool>();
    private List<Type> extensionTypes = new List<Type>();
    private void OnEnable()
    {
        extensionTypes.Clear();
        extensionToggles.Clear();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(PlayerExtension)) && !type.IsAbstract)
                {
                    extensionTypes.Add(type);
                    extensionToggles[type.Name] = ((PlayerController)target).GetComponent(type) != null;
                }
            }
        }
    }
    public override void OnInspectorGUI()
    {
        SerializedObject serializedObject = new SerializedObject(target);
        PlayerController player = (PlayerController)target;
        serializedObject.Update();

        #region camera
        // Draw "Cameras & Settings" header
        EditorGUILayout.LabelField("Cameras & Settings", EditorStyles.boldLabel);

        SerializedProperty cameraTypeProp = serializedObject.FindProperty("cameraType");
        EditorGUILayout.PropertyField(cameraTypeProp);

        SerializedProperty cameraFOVProp = serializedObject.FindProperty("cameraFOV");
        EditorGUILayout.PropertyField(cameraFOVProp);

        if (cameraTypeProp.enumValueIndex == (int)PlayerController.CameraType.ThirdPerson)
        {
            SerializedProperty cameraOffsetXProp = serializedObject.FindProperty("cameraOffsetX");
            SerializedProperty cameraOffsetYProp = serializedObject.FindProperty("cameraOffsetY");
            SerializedProperty cameraLookUpProp = serializedObject.FindProperty("cameraLookUp");

            EditorGUILayout.PropertyField(cameraOffsetXProp);
            EditorGUILayout.PropertyField(cameraOffsetYProp);
            EditorGUILayout.PropertyField(cameraLookUpProp);
        }
        if (cameraTypeProp.enumValueIndex == (int)PlayerController.CameraType.FirstPerson)
        {
            SerializedProperty cameraShakeProp = serializedObject.FindProperty("cameraShake");

            EditorGUILayout.PropertyField(cameraShakeProp);
        }

        // Draw remaining fields except the ones handled above
        SerializedProperty property = serializedObject.GetIterator();
        property.NextVisible(true);

        while (property.NextVisible(false))
        {
            if (property.name != "cameraType" && property.name != "cameraFOV" &&
                property.name != "cameraOffsetX" && property.name != "cameraOffsetY" && property.name != "cameraLookUp" && property.name != "cameraShake")
            {
                EditorGUILayout.PropertyField(property);
            }
        }
        #endregion

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player Action", EditorStyles.boldLabel);

        foreach (Type extensionType in extensionTypes)
        {
            string effectName = extensionType.Name;
            bool currentToggle = extensionToggles[effectName];
            bool newToggle = EditorGUILayout.Toggle(effectName, currentToggle);

            if (newToggle != currentToggle)
            {
                extensionToggles[effectName] = newToggle;
                ToggleEffect(player, extensionType, newToggle);
            }
        }
        if (GUI.changed)
        {
            player.RefreshExtension();
            EditorUtility.SetDirty(player);
        }
        #region Do Not Touch
        // Foldout for "DO NOT TOUCH" section
        showDoNotTouch = EditorGUILayout.Foldout(showDoNotTouch, "DO NOT TOUCH", true);
        if (showDoNotTouch)
        {
            SerializedProperty cameraProp = serializedObject.FindProperty("camera");
            SerializedProperty fpsCameraProp = serializedObject.FindProperty("fpsCamera");
            SerializedProperty tpsCameraProp = serializedObject.FindProperty("tpsCamera");
            SerializedProperty tpsCameraPivotProp = serializedObject.FindProperty("tpsCameraPivot");

            EditorGUILayout.PropertyField(cameraProp);
            EditorGUILayout.PropertyField(fpsCameraProp);
            EditorGUILayout.PropertyField(tpsCameraProp);
            EditorGUILayout.PropertyField(tpsCameraPivotProp);
        }
        #endregion
        serializedObject.ApplyModifiedProperties();
    }

    private void ToggleEffect(PlayerController interactable, Type effectType, bool enable)
    {
        if (enable)
        {
            if (interactable.GetComponent(effectType) == null)
            {
                interactable.gameObject.AddComponent(effectType);
            }
        }
        else
        {
            Component effect = interactable.GetComponent(effectType);
            if (effect != null)
            {
                DestroyImmediate(effect);
            }
        }
    }

}
