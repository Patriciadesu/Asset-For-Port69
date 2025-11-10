using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor for KillerAI to enhance Unity Inspector usability.
/// Place this file in an "Editor" folder in your project.
/// </summary>
[CustomEditor(typeof(KillerAI))]
public class KillerAIEditor : Editor
{
    private KillerAI killer;
    private SerializedProperty currentStateProp;
    private SerializedProperty targetPlayerProp;
    private SerializedProperty chaseRangeProp;
    private SerializedProperty attackRangeProp;
    private SerializedProperty patrolSpeedProp;
    private SerializedProperty chaseSpeedProp;
    private SerializedProperty attackDurationProp;
    private SerializedProperty attackCooldownProp;
    private SerializedProperty modulesProp;
    
    // Chase difficulty properties
    private SerializedProperty difficultyProp;
    private SerializedProperty customChaseSpeedProp;
    private SerializedProperty customRotationSpeedProp;
    private SerializedProperty customDetectionRangeProp;
    private SerializedProperty customStopDistanceProp;
    private SerializedProperty customInaccuracyRadiusProp;
    private SerializedProperty customRepathIntervalProp;
    private SerializedProperty customRepathJitterProp;
    private SerializedProperty customAgentAngularSpeedProp;
    private SerializedProperty customAgentAccelerationProp;
    private SerializedProperty customAvoidanceProp;
    private SerializedProperty customAvoidancePriorityProp;

    private bool showModulesList = true;
    private bool showStateControls = true;
    private bool showChaseDifficulty = true;

    private void OnEnable()
    {
        killer = (KillerAI)target;

        // Cache serialized properties
        currentStateProp = serializedObject.FindProperty("currentState");
        targetPlayerProp = serializedObject.FindProperty("TargetPlayer");
        chaseRangeProp = serializedObject.FindProperty("ChaseRange");
        attackRangeProp = serializedObject.FindProperty("AttackRange");
        patrolSpeedProp = serializedObject.FindProperty("PatrolSpeed");
        chaseSpeedProp = serializedObject.FindProperty("ChaseSpeed");
        attackDurationProp = serializedObject.FindProperty("AttackDuration");
        attackCooldownProp = serializedObject.FindProperty("AttackCooldown");
        modulesProp = serializedObject.FindProperty("modules");
        
        // Initialize chase difficulty properties
        difficultyProp = serializedObject.FindProperty("difficulty");
        customChaseSpeedProp = serializedObject.FindProperty("customChaseSpeed");
        customRotationSpeedProp = serializedObject.FindProperty("customRotationSpeed");
        customDetectionRangeProp = serializedObject.FindProperty("customDetectionRange");
        customStopDistanceProp = serializedObject.FindProperty("customStopDistance");
        customInaccuracyRadiusProp = serializedObject.FindProperty("customInaccuracyRadius");
        customRepathIntervalProp = serializedObject.FindProperty("customRepathInterval");
        customRepathJitterProp = serializedObject.FindProperty("customRepathJitter");
        customAgentAngularSpeedProp = serializedObject.FindProperty("customAgentAngularSpeed");
        customAgentAccelerationProp = serializedObject.FindProperty("customAgentAcceleration");
        customAvoidanceProp = serializedObject.FindProperty("customAvoidance");
        customAvoidancePriorityProp = serializedObject.FindProperty("customAvoidancePriority");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Custom header
        EditorGUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("KILLER AI CONTROLLER", titleStyle);
        EditorGUILayout.Space(5);

        // State Machine Section
        DrawStateMachineSection();
        EditorGUILayout.Space(10);

        // Runtime State Controls (Play Mode Only)
        if (Application.isPlaying)
        {
            DrawRuntimeControls();
            EditorGUILayout.Space(10);
        }

        // Targeting Section
        DrawTargetingSection();
        EditorGUILayout.Space(10);

        // Movement Section
        DrawMovementSection();
        EditorGUILayout.Space(10);

        // Chase Difficulty Section
        DrawChaseDifficultySection();
        EditorGUILayout.Space(10);

        // Attack Section
        DrawAttackSection();
        EditorGUILayout.Space(10);

        // Modules Section
        DrawModulesSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStateMachineSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("STATE MACHINE", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Current state with color coding
        EditorGUI.BeginDisabledGroup(true);
        Color stateColor = GetStateColor(killer.CurrentState);
        GUI.backgroundColor = stateColor;
        EditorGUILayout.PropertyField(currentStateProp, new GUIContent("Current State"));
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawRuntimeControls()
    {
        showStateControls = EditorGUILayout.BeginFoldoutHeaderGroup(showStateControls, "Runtime Controls");

        if (showStateControls)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("Manually trigger state changes during Play Mode", MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("→ IDLE", GUILayout.Height(30)))
            {
                killer.ChangeState(EnemyState.Idle);
            }

            if (GUILayout.Button("→ PATROL", GUILayout.Height(30)))
            {
                killer.ChangeState(EnemyState.Patrol);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("→ CHASE", GUILayout.Height(30)))
            {
                killer.ChangeState(EnemyState.Chase);
            }

            if (GUILayout.Button("→ ATTACK", GUILayout.Height(30)))
            {
                killer.ChangeState(EnemyState.Attack);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawTargetingSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("TARGETING", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Target Player field with Auto-Find button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(targetPlayerProp, new GUIContent("Target Player", "The player to target and damage"));
        if (GUILayout.Button("🔍 Find", GUILayout.Width(60), GUILayout.Height(18)))
        {
            killer.FindPlayer();
            serializedObject.Update();
            EditorUtility.SetDirty(killer);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(chaseRangeProp, new GUIContent("Chase Range", "Distance at which AI starts chasing the target"));
        EditorGUILayout.PropertyField(attackRangeProp, new GUIContent("Attack Range", "Distance at which AI can attack"));

        // Show distance to target if available
        if (Application.isPlaying && killer.TargetPlayer != null)
        {
            float distance = Vector3.Distance(killer.transform.position, killer.TargetPlayer.transform.position);
            bool inChase = distance <= killer.ChaseRange;
            bool inAttack = distance <= killer.AttackRange;
            string healthInfo = killer.TargetPlayer.Stat != null ? $"  |  Player HP: {killer.TargetPlayer.Stat.currenthealth:F0}/{killer.TargetPlayer.Stat.maxhealth:F0}" : "";
            EditorGUILayout.HelpBox($"Distance: {distance:F2}m  |  Chase: {(inChase ? "✓" : "✗")}  |  Attack: {(inAttack ? "✓" : "✗")}{healthInfo}", MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMovementSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("MOVEMENT", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(patrolSpeedProp, new GUIContent("Patrol Speed"));
        EditorGUILayout.PropertyField(chaseSpeedProp, new GUIContent("Chase Speed"));

        EditorGUILayout.HelpBox("Patrol is handled by PatrolModule. Add PatrolModule to assign waypoints.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void DrawAttackSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("ATTACK", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(attackDurationProp, new GUIContent("Attack Duration"));
        EditorGUILayout.PropertyField(attackCooldownProp, new GUIContent("Attack Cooldown"));

        EditorGUILayout.EndVertical();
    }

    private void DrawChaseDifficultySection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("CHASE DIFFICULTY PRESETS", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Difficulty dropdown
        EditorGUILayout.PropertyField(difficultyProp, new GUIContent("Difficulty Mode"));
        KillerAI.ChaseDifficulty currentDifficulty = (KillerAI.ChaseDifficulty)difficultyProp.enumValueIndex;

        EditorGUILayout.Space(10);

        // Quick preset buttons
        EditorGUILayout.LabelField("Quick Apply:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🟢 Easy", GUILayout.Height(28)))
        {
            difficultyProp.enumValueIndex = (int)KillerAI.ChaseDifficulty.Easy;
            serializedObject.ApplyModifiedProperties();
            killer.SetDifficulty(KillerAI.ChaseDifficulty.Easy);
        }
        if (GUILayout.Button("🟡 Medium", GUILayout.Height(28)))
        {
            difficultyProp.enumValueIndex = (int)KillerAI.ChaseDifficulty.Medium;
            serializedObject.ApplyModifiedProperties();
            killer.SetDifficulty(KillerAI.ChaseDifficulty.Medium);
        }
        if (GUILayout.Button("🔴 Hard", GUILayout.Height(28)))
        {
            difficultyProp.enumValueIndex = (int)KillerAI.ChaseDifficulty.Hard;
            serializedObject.ApplyModifiedProperties();
            killer.SetDifficulty(KillerAI.ChaseDifficulty.Hard);
        }
        if (GUILayout.Button("⚙️ Custom", GUILayout.Height(28)))
        {
            difficultyProp.enumValueIndex = (int)KillerAI.ChaseDifficulty.Custom;
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Display preset info
        switch (currentDifficulty)
        {
            case KillerAI.ChaseDifficulty.Easy:
                EditorGUILayout.HelpBox(
                    "🟢 EASY MODE\n" +
                    "• Chase Speed: 4 m/s\n" +
                    "• Detection Range: 15m\n" +
                    "• Path Accuracy: Low (4.5)\n" +
                    "• Turn Speed: 120°/s\n" +
                    "• High hesitation & confusion\n\n" +
                    "Perfect for new players!",
                    MessageType.Info);
                break;

            case KillerAI.ChaseDifficulty.Medium:
                EditorGUILayout.HelpBox(
                    "🟡 MEDIUM MODE\n" +
                    "• Chase Speed: 6 m/s\n" +
                    "• Detection Range: 12m\n" +
                    "• Path Accuracy: Moderate (2.5)\n" +
                    "• Turn Speed: 200°/s\n" +
                    "• Balanced challenge\n\n" +
                    "Recommended difficulty!",
                    MessageType.Info);
                break;

            case KillerAI.ChaseDifficulty.Hard:
                EditorGUILayout.HelpBox(
                    "🔴 HARD MODE\n" +
                    "• Chase Speed: 8 m/s\n" +
                    "• Detection Range: 20m\n" +
                    "• Path Accuracy: High (0.8)\n" +
                    "• Turn Speed: 360°/s\n" +
                    "• Minimal hesitation\n\n" +
                    "For experienced players only!",
                    MessageType.Warning);
                break;

            case KillerAI.ChaseDifficulty.Custom:
                EditorGUILayout.HelpBox(
                    "⚙️ CUSTOM MODE\n" +
                    "Adjust all parameters below to create your own difficulty level.",
                    MessageType.Info);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Chase Parameters", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(customChaseSpeedProp, new GUIContent("Chase Speed (m/s)"));
                EditorGUILayout.PropertyField(customDetectionRangeProp, new GUIContent("Detection Range (m)"));
                EditorGUILayout.PropertyField(customStopDistanceProp, new GUIContent("Stop Distance (m)"));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Pathfinding Parameters", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(customInaccuracyRadiusProp, new GUIContent("Path Inaccuracy"));
                EditorGUILayout.PropertyField(customRepathIntervalProp, new GUIContent("Repath Interval (s)"));
                EditorGUILayout.PropertyField(customRepathJitterProp, new GUIContent("Repath Jitter"));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("NavMeshAgent Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(customAgentAngularSpeedProp, new GUIContent("Angular Speed (°/s)"));
                EditorGUILayout.PropertyField(customAgentAccelerationProp, new GUIContent("Acceleration"));
                EditorGUILayout.PropertyField(customAvoidanceProp, new GUIContent("Obstacle Avoidance"));
                EditorGUILayout.PropertyField(customAvoidancePriorityProp, new GUIContent("Avoidance Priority (0-99)"));
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawModulesSection()
    {
        EditorGUILayout.BeginVertical("box");

        // Compute live modules to reflect current components in Editor
        var liveModules = killer.GetComponents<EnemyModule>();
        int moduleCount = liveModules?.Length ?? 0;
        string moduleHeader = $"MODULES ({moduleCount})";

        showModulesList = EditorGUILayout.BeginFoldoutHeaderGroup(showModulesList, moduleHeader);

        if (showModulesList)
        {
            EditorGUILayout.Space(5);

            if (moduleCount == 0)
            {
                EditorGUILayout.HelpBox("No modules attached. Add EnemyModule scripts to extend AI capabilities.", MessageType.Info);
            }
            else
            {
                // Display each module with status
                for (int i = 0; i < moduleCount; i++)
                {
                    var module = liveModules[i];
                    if (module != null)
                    {
                        EditorGUILayout.BeginHorizontal("box");

                        // Active toggle
                        module.IsActive = EditorGUILayout.Toggle(module.IsActive, GUILayout.Width(20));

                        // Module name as clickable button (warps focus to that component)
                        string moduleName = module.GetType().Name;
                        Color statusColor = module.IsActive ? Color.green : Color.gray;

                        GUIStyle clickableStyle = new GUIStyle(EditorStyles.label);
                        clickableStyle.normal.textColor = statusColor;
                        clickableStyle.fontStyle = FontStyle.Bold;

                        if (GUILayout.Button($"[{(module.IsActive ? "●" : "○")}] {moduleName}", clickableStyle))
                        {
                            // Select and ping the specific component; Inspector will scroll to it
                            Selection.activeObject = module;
                            InternalEditorUtility.SetIsInspectorExpanded(module, true);
                            EditorGUIUtility.PingObject(module);
                            EditorApplication.delayCall += () =>
                            {
                                InternalEditorUtility.SetIsInspectorExpanded(module, true);
                                // Intentionally not forcing Inspector focus to avoid using internal types.
                            };
                        }

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            Undo.RecordObject(killer.gameObject, "Remove Module");
                            Undo.DestroyObjectImmediate(module);
                            var ai = killer.GetComponent<KillerAI>();
                            if (ai != null)
                            {
                                ai.RefreshModulesInEditor();
                                EditorUtility.SetDirty(ai);
                            }
                            EditorUtility.SetDirty(killer.gameObject);
                            GUIUtility.ExitGUI();
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.Space(5);

            // Add module button
            if (GUILayout.Button("+ Add New Module", GUILayout.Height(25)))
            {
                ShowModuleMenu();
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.EndVertical();
    }

    private void ShowModuleMenu()
    {
        GenericMenu menu = new GenericMenu();

        // Find all EnemyModule types with error handling
        List<Type> moduleTypes = new List<Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(EnemyModule)));

                moduleTypes.AddRange(types);
            }
            catch (Exception)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        // Sort alphabetically
        moduleTypes = moduleTypes.OrderBy(t => t.Name).ToList();

        foreach (var type in moduleTypes)
        {
            // Check if module is already attached
            bool hasModule = killer.gameObject.GetComponent(type) != null;

            if (hasModule)
            {
                menu.AddDisabledItem(new GUIContent(type.Name + " (Already Added)"));
            }
            else
            {
                menu.AddItem(new GUIContent(type.Name), false, () => {
                    Undo.RecordObject(killer.gameObject, "Add Module");
                    killer.gameObject.AddComponent(type);
                    // Refresh the modules list on KillerAI to reflect changes immediately
                    var ai = killer.GetComponent<KillerAI>();
                    if (ai != null)
                    {
                        ai.RefreshModulesInEditor();
                        EditorUtility.SetDirty(ai);
                    }
                    EditorUtility.SetDirty(killer.gameObject);
                });
            }
        }

        if (moduleTypes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No EnemyModule scripts found"));
        }

        menu.ShowAsContext();
    }


    private Color GetStateColor(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return new Color(0.7f, 0.7f, 0.7f);
            case EnemyState.Patrol:
                return new Color(0.5f, 0.8f, 1f);
            case EnemyState.Chase:
                return new Color(1f, 0.8f, 0.3f);
            case EnemyState.Attack:
                return new Color(1f, 0.3f, 0.3f);
            default:
                return Color.white;
        }
    }

    // Scene view visualization
    private void OnSceneGUI()
    {
        if (!Application.isPlaying)
            return;

        // Draw state label above AI
        Handles.BeginGUI();
        Vector3 screenPos = HandleUtility.WorldToGUIPoint(killer.transform.position + Vector3.up * 3f);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetStateColor(killer.CurrentState);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 14;

        GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 25, 100, 50),
                  killer.CurrentState.ToString().ToUpper(), style);

        Handles.EndGUI();
    }
}