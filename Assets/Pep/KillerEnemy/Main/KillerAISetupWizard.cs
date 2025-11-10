using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor wizard for quick KillerAI setup and testing.
/// Access via: Tools > Killer AI > Setup Wizard
/// Place this file in an "Editor" folder in your project.
/// </summary>
public class KillerAISetupWizard : EditorWindow
{
    private GameObject killerPrefab;
    private GameObject targetObject;
    private int waypointCount = 4;
    private float waypointRadius = 10f;

    // Dynamic module system
    private List<Type> availableModules = new List<Type>();
    private Dictionary<Type, bool> moduleSelection = new Dictionary<Type, bool>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Killer AI/Setup Wizard")]
    public static void ShowWindow()
    {
        KillerAISetupWizard window = GetWindow<KillerAISetupWizard>("Killer AI Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        // Find all EnemyModule types when window opens
        FindAllModules();
    }

    private void FindAllModules()
    {
        availableModules.Clear();
        moduleSelection.Clear();

        // Get all assemblies and find types that inherit from EnemyModule
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EnemyModule)));

                foreach (var type in types)
                {
                    availableModules.Add(type);
                    moduleSelection[type] = false; // Default to unchecked
                }
            }
            catch (Exception)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        // Sort modules alphabetically
        availableModules = availableModules.OrderBy(t => t.Name).ToList();

        Debug.Log($"[Setup Wizard] Found {availableModules.Count} EnemyModule types");
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("KILLER AI SETUP WIZARD", titleStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This wizard will help you quickly set up a Killer AI with modules and waypoints.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Player Status Check
        EditorGUILayout.BeginVertical("box");
        // Validate Player.Instance is actually valid and in the scene
        bool playerFound = Player.Instance != null && 
                          Player.Instance.gameObject != null && 
                          Player.Instance.gameObject.scene.IsValid();
        bool targetAssigned = targetObject != null;
        
        if (playerFound)
        {
            EditorGUILayout.HelpBox(
                "✓ Player found in scene: " + Player.Instance.name,
                MessageType.Info
            );
        }
        else if (targetAssigned)
        {
            bool hasPlayerScript = targetObject.GetComponent<Player>() != null;
            if (hasPlayerScript)
            {
                EditorGUILayout.HelpBox(
                    "✓ Target assigned with Player script: " + targetObject.name,
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "🛠 Target assigned: " + targetObject.name + "\n\n" +
                    "Player script will be added automatically when you create the AI.",
                    MessageType.Info
                );
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "⚠ No target assigned!\n\n" +
                "Either:\n" +
                "• Click 'Find Player' to locate Player.Instance, OR\n" +
                "• Assign a Target Object (Player script will be added automatically)",
                MessageType.Warning
            );
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Killer Setup Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("1. Killer Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        killerPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Killer Model (Optional)", "Drag a model here, or leave empty to create a capsule"),
            killerPrefab,
            typeof(GameObject),
            false
        );

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Target Object", "The object the AI will chase (usually the player)"),
            targetObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Waypoint Setup Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("2. Patrol Waypoints", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        waypointCount = EditorGUILayout.IntSlider(
            new GUIContent("Waypoint Count", "Number of patrol waypoints to create"),
            waypointCount,
            2,
            10
        );

        waypointRadius = EditorGUILayout.Slider(
            new GUIContent("Waypoint Radius", "Distance from center for waypoint placement"),
            waypointRadius,
            5f,
            50f
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Module Selection Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("3. Modules to Add", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (availableModules.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No EnemyModule scripts found in the project. Create scripts that inherit from EnemyModule to see them here.",
                MessageType.Warning
            );

            if (GUILayout.Button("Refresh Module List"))
            {
                FindAllModules();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(100)))
            {
                foreach (var key in moduleSelection.Keys.ToList())
                {
                    moduleSelection[key] = true;
                }
            }
            if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
            {
                foreach (var key in moduleSelection.Keys.ToList())
                {
                    moduleSelection[key] = false;
                }
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                FindAllModules();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Display all found modules with checkboxes
            foreach (var moduleType in availableModules)
            {
                bool currentValue = moduleSelection[moduleType];
                bool newValue = EditorGUILayout.Toggle(
                    new GUIContent(FormatModuleName(moduleType.Name), $"Type: {moduleType.FullName}"),
                    currentValue
                );
                moduleSelection[moduleType] = newValue;
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        // Create Button
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("CREATE KILLER AI", GUILayout.Height(40)))
        {
            // Check if Player exists or if target object is assigned
            bool validPlayerExists = Player.Instance != null && 
                                    Player.Instance.gameObject != null && 
                                    Player.Instance.gameObject.scene.IsValid();
            
            if (!validPlayerExists && targetObject == null)
            {
                EditorUtility.DisplayDialog(
                    "No Target Found",
                    "No target found for the Killer AI!\n\n" +
                    "Either:\n" +
                    "• Have a Player in the scene (with Player script), OR\n" +
                    "• Assign a Target Object (Player script will be added automatically)\n\n" +
                    "Please add/assign a target, then try again.",
                    "OK"
                );
                return;
            }
            
            CreateKillerAI();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Quick Actions Section
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Find Player"))
        {
            // Try using Player.Instance first (recommended)
            if (Player.Instance != null && Player.Instance.gameObject != null && Player.Instance.gameObject.scene.IsValid())
            {
                targetObject = Player.Instance.gameObject;
                Debug.Log("[Setup Wizard] Found player via Player.Instance");
            }
            else
            {
                // Fallback to tag search
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetObject = player;
                    Debug.Log("[Setup Wizard] Found player object by tag");
                }
                else
                {
                    Debug.LogWarning("[Setup Wizard] No Player.Instance or 'Player' tag found");
                    EditorUtility.DisplayDialog(
                        "Player Not Found",
                        "No Player found in the scene!\n\n" +
                        "Make sure you have a GameObject with the Player script in your scene.",
                        "OK"
                    );
                }
            }
        }

        if (GUILayout.Button("Create Test Target"))
        {
            CreateTestTarget();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();
    }

    private string FormatModuleName(string typeName)
    {
        // Convert "ExampleVisionModule" to "Example Vision Module"
        string result = System.Text.RegularExpressions.Regex.Replace(
            typeName,
            "(\\B[A-Z])",
            " $1"
        );

        // Remove "Module" suffix if present
        if (result.EndsWith(" Module"))
        {
            result = result.Substring(0, result.Length - 7);
        }

        return result.Trim();
    }

    private void CreateKillerAI()
    {
        // Validate KillerAI script exists
        if (typeof(KillerAI) == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "KillerAI script not found in project. Please ensure the KillerAI.cs script exists.",
                "OK"
            );
            return;
        }

        // Create root GameObject
        GameObject killer;

        if (killerPrefab != null)
        {
            killer = Instantiate(killerPrefab);
            killer.name = "KillerAI";
        }
        else
        {
            killer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            killer.name = "KillerAI";
            killer.transform.localScale = new Vector3(1f, 2f, 1f);
        }

        // Position at origin
        killer.transform.position = Vector3.zero;

        // Add CharacterController if not present
        if (killer.GetComponent<CharacterController>() == null)
        {
            CharacterController cc = killer.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
        }

        // Add KillerAI component
        KillerAI killerAI = killer.AddComponent<KillerAI>();

        // Set target - prioritize Player.Instance, then targetObject
        if (Player.Instance != null && Player.Instance.gameObject != null && Player.Instance.gameObject.scene.IsValid())
        {
            killerAI.TargetPlayer = Player.Instance;
            Debug.Log($"[Setup Wizard] Assigned Player.Instance as target: {Player.Instance.name}");
        }
        else if (targetObject != null)
        {
            // Try to get Player component from manually assigned target
            Player player = targetObject.GetComponent<Player>();
            if (player == null)
            {
                // Target doesn't have Player component - add it automatically
                Debug.Log($"[Setup Wizard] Target object '{targetObject.name}' doesn't have Player script. Adding it now...");
                player = targetObject.AddComponent<Player>();
                Debug.Log($"[Setup Wizard] Added Player script to '{targetObject.name}'");
            }
            
            killerAI.TargetPlayer = player;
            Debug.Log($"[Setup Wizard] Assigned target: {player.name}");
        }
        else
        {
            Debug.LogWarning("[Setup Wizard] No player target assigned. Assign one manually in the inspector.");
        }

        // Create waypoints
        Transform[] waypoints = CreateWaypoints(killer.transform.position);

        // Ensure a PatrolModule exists and assign points to it
        var patrolModule = killer.GetComponent<PatrolModule>();
        if (patrolModule == null)
        {
            patrolModule = killer.AddComponent<PatrolModule>();
            Debug.Log("[Setup Wizard] Added PatrolModule to handle waypoints");
        }
        patrolModule.Points = waypoints;

        // Add selected modules
        int modulesAdded = 0;
        foreach (var kvp in moduleSelection)
        {
            if (kvp.Value) // If checkbox is checked
            {
                try
                {
                    killer.AddComponent(kvp.Key);
                    modulesAdded++;
                    Debug.Log($"[Setup Wizard] Added module: {kvp.Key.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Setup Wizard] Failed to add module {kvp.Key.Name}: {e.Message}");
                }
            }
        }

        // Select the created object
        Selection.activeGameObject = killer;

        // Focus scene view on it
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log($"[Setup Wizard] Killer AI created with {waypoints.Length} waypoints and {modulesAdded} modules");

        EditorUtility.DisplayDialog(
            "Success",
            $"Killer AI created successfully!\n\nWaypoints: {waypoints.Length}\nModules: {modulesAdded}",
            "OK"
        );
    }

    private Transform[] CreateWaypoints(Vector3 center)
    {
        GameObject waypointParent = new GameObject("AI_Waypoints");
        waypointParent.transform.position = center;

        Transform[] waypoints = new Transform[waypointCount];

        for (int i = 0; i < waypointCount; i++)
        {
            float angle = (360f / waypointCount) * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 position = center + new Vector3(
                Mathf.Cos(radians) * waypointRadius,
                0f,
                Mathf.Sin(radians) * waypointRadius
            );

            GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waypoint.name = $"Waypoint_{i + 1}";
            waypoint.transform.position = position;
            waypoint.transform.localScale = Vector3.one * 0.5f;
            waypoint.transform.SetParent(waypointParent.transform);

            // Color waypoint
            Renderer renderer = waypoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.cyan;
                renderer.material = mat;
            }

            // Remove collider
            Collider collider = waypoint.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            waypoints[i] = waypoint.transform;
        }

        return waypoints;
    }

    private void CreateTestTarget()
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = "TestTarget";
        target.tag = "Player";
        target.transform.position = Vector3.forward * 15f;

        // Color target
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            renderer.material = mat;
        }

        targetObject = target;
        Selection.activeGameObject = target;

        Debug.Log("[Setup Wizard] Test target created at " + target.transform.position);
    }
}