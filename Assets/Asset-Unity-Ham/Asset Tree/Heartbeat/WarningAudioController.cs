using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeartbeatThemeSystem
{
    [DisallowMultipleComponent]
    public class WarningAudioController : MonoBehaviour
    {
        [Header("Heartbeat")]
        [SerializeField] private bool enableHeartbeat = true;

        [Header("Chase Theme")]
        [SerializeField] private bool enableChaseTheme = true;

        [SerializeField, Tooltip("Optional override when running outside play mode.")]
        private bool applyInEditMode = true;

        private Type heartbeatType;
        private Type chaseThemeType;

        private void OnValidate()
        {
            ResolveTypes();
            ApplyConfiguration();
        }

        private void Awake()
        {
            ResolveTypes();
            ApplyConfiguration();
        }

        private void ResolveTypes()
        {
            heartbeatType = heartbeatType ?? FindType("HeartbeatThemeSystem.HeartbeatController");
            chaseThemeType = chaseThemeType ?? FindType("HeartbeatThemeSystem.ChaseThemeController");
        }

        private Type FindType(string qualifiedName)
        {
            return Type.GetType(qualifiedName) ??
                   Type.GetType($"{qualifiedName}, Assembly-CSharp");
        }

        private void ApplyConfiguration()
        {
            if (!Application.isPlaying && !applyInEditMode) return;

            ToggleBehaviour(heartbeatType, heartbeatType != null && enableHeartbeat);
            ToggleBehaviour(chaseThemeType, chaseThemeType != null && enableChaseTheme);
        }

        private void ToggleBehaviour(Type type, bool shouldBeEnabled)
        {
            if (type == null) return;

            var existing = GetComponent(type);
            bool isEditor = !Application.isPlaying;

            if (shouldBeEnabled && existing == null)
            {
                if (isEditor)
                {
#if UNITY_EDITOR
                    var go = gameObject;
                    EditorApplication.delayCall += () =>
                    {
                        if (!go) return;
                        if (!go.GetComponent(type))
                        {
                            Undo.AddComponent(go, type);
                        }
                    };
#endif
                }
                else
                {
                    gameObject.AddComponent(type);
                }
            }
            else if (!shouldBeEnabled && existing != null)
            {
                if (isEditor)
                {
#if UNITY_EDITOR
                    var toRemove = existing;
                    EditorApplication.delayCall += () =>
                    {
                        if (toRemove)
                        {
                            Undo.DestroyObjectImmediate(toRemove);
                        }
                    };
#endif
                }
                else
                {
                    Destroy(existing);
                }
            }
        }

        public bool HasHeartbeatSupport => heartbeatType != null;
        public bool HasChaseThemeSupport => chaseThemeType != null;

#if UNITY_EDITOR
        [CustomEditor(typeof(WarningAudioController))]
        private class WarningAudioControllerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                var controller = (WarningAudioController)target;
                controller.ResolveTypes();

                if (!controller.HasHeartbeatSupport && !controller.HasChaseThemeSupport)
                {
                    EditorGUILayout.HelpBox(
                        "No heartbeat or chase theme scripts found in the project. Add one of the scripts from this package to enable toggles.",
                        MessageType.Warning);
                }

                if (controller.HasHeartbeatSupport)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(enableHeartbeat)));
                }

                if (controller.HasChaseThemeSupport)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(enableChaseTheme)));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(applyInEditMode)));
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}

