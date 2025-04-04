using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    private Dictionary<string, bool> effectToggles = new Dictionary<string, bool>();
    private List<Type> effectTypes = new List<Type>();

    public override void OnInspectorGUI()
    {
        InteractableObject interactable = (InteractableObject)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        foreach (Type effectType in effectTypes)
        {
            string effectName = effectType.Name;
            bool currentToggle = effectToggles[effectName];
            bool newToggle = EditorGUILayout.Toggle(effectName, currentToggle);

            if (newToggle != currentToggle)
            {
                effectToggles[effectName] = newToggle;
                ToggleEffect(interactable, effectType, newToggle);
            }
        }

        if (GUI.changed)
        {
            interactable.RefreshEffects();
            EditorUtility.SetDirty(interactable);
        }
    }

    private void ToggleEffect(InteractableObject interactable, Type effectType, bool enable)
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

    void OnEnable()
    {
        // Find all types that inherit from ObjectEffect
        effectTypes.Clear();
        effectToggles.Clear();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ObjectEffect)) && !type.IsAbstract)
                {
                    effectTypes.Add(type);
                    effectToggles[type.Name] = ((InteractableObject)target).GetComponent(type) != null;
                }
            }
        }
    }
}