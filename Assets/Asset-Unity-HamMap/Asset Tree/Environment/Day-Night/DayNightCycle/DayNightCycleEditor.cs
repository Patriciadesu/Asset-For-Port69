#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(DayNightCycle))]
public class DayNightCycleEditor : Editor
{
    private DayNightCycle dayNightCycle;
    private bool showTimeControls = true;
    private bool showLightingSettings = true;
    private bool showSkySettings = true;
    private bool showEvents = false;
    private bool showQuickSetup = false;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    void OnEnable()
    {
        dayNightCycle = (DayNightCycle)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InitializeStyles();

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Max(140f, EditorGUIUtility.currentViewWidth * 0.4f);

        DrawHeader();
        DrawQuickSetup();
        DrawTimeControls();
        DrawLightingSettings();
        DrawSkySettings();
        DrawEvents();

        EditorGUIUtility.labelWidth = prevLabelWidth;

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed && Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
        }
    }

    void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }

    void DrawHeader()
    {
        EditorGUILayout.Space(10);

        // Title
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("🌅 Day/Night Cycle Controller 🌙", headerStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Current time display (only in play mode)
        if (Application.isPlaying)
        {
            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Time:", GUILayout.Width(100));
            EditorGUILayout.LabelField(dayNightCycle.TimeString, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(dayNightCycle.IsDay ? "☀️ Day" : "🌙 Night", GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Day: {dayNightCycle.DayCount}", EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        EditorGUILayout.Space(10);
    }

    void DrawQuickSetup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showQuickSetup = EditorGUILayout.Foldout(showQuickSetup, "🚀 Default Setup", true, EditorStyles.foldoutHeader);

        if (showQuickSetup)
        {
            // EditorGUILayout.HelpBox("This is a default setup", MessageType.Info);

            // First row: Auto-Find Lights, Auto-Find Skybox
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 Auto-Find Lights", buttonStyle))
            {
                AutoFindLights();
            }
            if (GUILayout.Button("🔍 Auto-Find Skybox", buttonStyle))
            {
                AutoFindSkyboxMaterial();
            }
            EditorGUILayout.EndHorizontal();

            // Second row: Setup Default Colors, Reset All
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎨 Setup Default Colors", buttonStyle))
            {
                SetupDefaultGradients();
            }
            if (GUILayout.Button("♻️ Reset All", buttonStyle))
            {
                ResetAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    void DrawTimeControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showTimeControls = EditorGUILayout.Foldout(showTimeControls, "⏰ Time Controls", true, EditorStyles.foldoutHeader);

        if (showTimeControls)
        {
            var settings = serializedObject.FindProperty("settings");

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("timeSpeed"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("startTime"));

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Runtime Controls:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("⏸️ Pause"))
                {
                    dayNightCycle.PauseTime();
                }
                if (GUILayout.Button("▶️ Resume"))
                {
                    dayNightCycle.ResumeTime();
                }
                if (GUILayout.Button("⏩ Speed x2"))
                {
                    dayNightCycle.SetTimeSpeed(2f);
                }
                if (GUILayout.Button("⏪ Speed x0.5"))
                {
                    dayNightCycle.SetTimeSpeed(0.5f);
                }
                EditorGUILayout.EndHorizontal();

                // Time scrubber
                EditorGUILayout.Space(5);
                float newTime = EditorGUILayout.Slider("Scrub Time", dayNightCycle.CurrentTime, 0f, 24f);
                if (newTime != dayNightCycle.CurrentTime)
                {
                    dayNightCycle.SetTime(newTime);
                }
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    void DrawLightingSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showLightingSettings = EditorGUILayout.Foldout(showLightingSettings, "💡 Lighting Settings", true, EditorStyles.foldoutHeader);

        if (showLightingSettings)
        {
            var settings = serializedObject.FindProperty("settings");

            // Sun Settings
            EditorGUILayout.LabelField("☀️ Sun Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("sunLight"));
            }
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("sunColor"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("sunIntensity"));
            }

            EditorGUILayout.Space(10);

            // Moon Settings
            EditorGUILayout.LabelField("🌙 Moon Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("moonLight"));
            }
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("moonColor"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("moonIntensity"));
            }

            if (settings.FindPropertyRelative("sunLight").objectReferenceValue == null ||
                settings.FindPropertyRelative("moonLight").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("💡 Tip: Use 'Auto-Find Lights' in Quick Setup to automatically assign lights!", MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    void DrawSkySettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showSkySettings = EditorGUILayout.Foldout(showSkySettings, "🌌 Sky & Atmosphere", true, EditorStyles.foldoutHeader);

        if (showSkySettings)
        {
            var settings = serializedObject.FindProperty("settings");

            // Skybox Settings
            EditorGUILayout.LabelField("🌌 Skybox", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("skyboxMaterial"));
            }
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("skyTint"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("skyExposure"));
            }
            if(settings.FindPropertyRelative("skyboxMaterial").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("💡 Tip: Use 'Auto-Find Skybox' in Quick Setup to automatically assign a skybox!", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Fog Settings
            EditorGUILayout.LabelField("🌫️ Fog", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("useFog"));

            if (settings.FindPropertyRelative("useFog").boolValue)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("fogColor"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("fogDensity"));
                }
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    void DrawEvents()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showEvents = EditorGUILayout.Foldout(showEvents, "🎯 Time Events", true, EditorStyles.foldoutHeader);

        if (showEvents)
        {
            EditorGUILayout.HelpBox("These events are triggered at specific times of day:", MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnSunrise"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnNoon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnSunset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnMidnight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnNewDay"));
        }

        EditorGUILayout.EndVertical();
    }

    void AutoFindLights()
    {
        var settings = serializedObject.FindProperty("settings");

        // Find active directional lights in the scene
        Light[] allLights = FindObjectsOfType<Light>();
        var directional = allLights.Where(l => l != null && l.type == LightType.Directional).ToList();

        if (directional.Count == 0)
        {
            Debug.LogWarning("No directional lights found! Please create directional lights for sun and moon.");
            return;
        }

        Light chosenSun = null;
        Light chosenMoon = null;

        // 1) Prefer RenderSettings.sun for sun
        if (RenderSettings.sun != null && directional.Contains(RenderSettings.sun))
        {
            chosenSun = RenderSettings.sun;
        }

        // 2) Name-based hinting
        if (chosenSun == null)
        {
            chosenSun = directional.FirstOrDefault(l => l.name.ToLowerInvariant().Contains("sun"));
        }
        if (chosenMoon == null)
        {
            chosenMoon = directional.FirstOrDefault(l => l.name.ToLowerInvariant().Contains("moon"));
        }

        // 3) Fallback to brightness ranking if still missing
        if (chosenSun == null)
        {
            chosenSun = directional.OrderByDescending(l => l.intensity).FirstOrDefault();
        }

        if (chosenMoon == null)
        {
            // pick a different light than sun; prefer the dimmer one
            var remaining = directional.Where(l => l != chosenSun).ToList();
            if (remaining.Count > 0)
            {
                // Prefer name-hinted moon among remaining
                chosenMoon = remaining.FirstOrDefault(l => l.name.ToLowerInvariant().Contains("moon"))
                             ?? remaining.OrderBy(l => l.intensity).FirstOrDefault();
            }
        }

        // Ensure sun and moon are not the same
        if (chosenMoon == chosenSun)
        {
            chosenMoon = null;
        }

        if (chosenSun != null)
        {
            settings.FindPropertyRelative("sunLight").objectReferenceValue = chosenSun;
            Debug.Log($"Assigned sun light: {chosenSun.name}");
        }

        if (chosenMoon != null)
        {
            settings.FindPropertyRelative("moonLight").objectReferenceValue = chosenMoon;
            Debug.Log($"Assigned moon light: {chosenMoon.name}");
        }
        else
        {
            if (directional.Count > 1)
            {
                Debug.LogWarning("Multiple directional lights found but couldn't confidently pick a moon light. Please assign the moon manually.");
            }
            else
            {
                Debug.LogWarning("Only one directional light found. Consider adding a separate directional light for the moon.");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void AutoFindSkyboxMaterial()
    {
        var settings = serializedObject.FindProperty("settings");
        Material currentSkybox = RenderSettings.skybox;
        if (currentSkybox != null)
        {
            settings.FindPropertyRelative("skyboxMaterial").objectReferenceValue = currentSkybox;
            Debug.Log($"Assigned current scene skybox: {currentSkybox.name}");
        }
        else
        {
            // Find all materials in the project
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            Material foundSkybox = null;
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null && mat.shader.name.Contains("Skybox"))
                {
                    foundSkybox = mat;
                    break;
                }
            }
            if (foundSkybox != null)
            {
                settings.FindPropertyRelative("skyboxMaterial").objectReferenceValue = foundSkybox;
                Debug.Log($"Found skybox material: {foundSkybox.name}");
            }
            else
            {
                Debug.LogWarning("No skybox material found in the project!");
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    void SetupDefaultGradients()
    {
        dayNightCycle.GetComponent<DayNightCycle>().SendMessage("SetupDefaultSunGradient", SendMessageOptions.DontRequireReceiver);
        dayNightCycle.GetComponent<DayNightCycle>().SendMessage("SetupDefaultSkyGradient", SendMessageOptions.DontRequireReceiver);
        dayNightCycle.GetComponent<DayNightCycle>().SendMessage("SetupDefaultFogGradient", SendMessageOptions.DontRequireReceiver);

        Debug.Log("Default color gradients have been set up!");
        EditorUtility.SetDirty(target);
    }

    void ResetAll()
    {
        Undo.RecordObject(dayNightCycle, "Reset DayNightCycle Settings");
        dayNightCycle.settings = new DayNightSettings();
        EditorUtility.SetDirty(dayNightCycle);
        serializedObject.Update();
        Debug.Log("All Day/Night Cycle settings have been reset to default.");
    }

    void SetTimePreset(float time)
    {
        if (Application.isPlaying)
        {
            dayNightCycle.SetTime(time);
        }
        else
        {
            var settings = serializedObject.FindProperty("settings");
            settings.FindPropertyRelative("startTime").floatValue = time;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

[CustomPropertyDrawer(typeof(DayNightSettings))]
public class DayNightSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif