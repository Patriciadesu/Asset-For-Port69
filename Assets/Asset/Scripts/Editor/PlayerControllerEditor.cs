using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    private bool showDoNotTouch = false; // Track foldout state

    public override void OnInspectorGUI()
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.Update();

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

        serializedObject.ApplyModifiedProperties();
    }
}
