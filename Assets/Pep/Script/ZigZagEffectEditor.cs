#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[CustomEditor(typeof(ZigZagEffect))]
public class ZigZagEffectEditor : Editor
{
    private ZigZagEffect zigzagEffect;
    private SerializedProperty waypointsProperty;
    private SerializedProperty showGizmosProperty;
    private SerializedProperty waypointColorProperty;
    private SerializedProperty pathColorProperty;
    private SerializedProperty waypointSizeProperty;
    private bool showWaypointSettings = true;
    private bool showGizmoSettings = true;
    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private void OnEnable()
    {
        zigzagEffect = (ZigZagEffect)target;
        waypointsProperty = serializedObject.FindProperty("waypoints");
        showGizmosProperty = serializedObject.FindProperty("showGizmos");
        waypointColorProperty = serializedObject.FindProperty("waypointColor");
        pathColorProperty = serializedObject.FindProperty("pathColor");
        waypointSizeProperty = serializedObject.FindProperty("waypointSize");
    }
    public override void OnInspectorGUI()
    {
        InitializeStyles();
        serializedObject.Update();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 ZigZag Waypoint Effect", headerStyle);
        EditorGUILayout.Space(5);
        DrawBasicSettings();
        EditorGUILayout.Space(10);
        DrawWaypointSettings();
        EditorGUILayout.Space(10);
        DrawGizmoSettings();
        EditorGUILayout.Space(10);
        DrawQuickActions();
        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }
    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }
    }
    private void DrawBasicSettings()
    {
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("usePhysics"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("warpToStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopPath"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
    private void DrawWaypointSettings()
    {
        showWaypointSettings = EditorGUILayout.Foldout(showWaypointSettings, $"🗺️ Waypoints ({waypointsProperty.arraySize})", true);
        if (showWaypointSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Index", GUILayout.Width(50));
            EditorGUILayout.LabelField("Local Position", GUILayout.Width(150));
            EditorGUILayout.LabelField("Wait Time", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(300));
            for (int i = 0; i < waypointsProperty.arraySize; i++)
            {
                DrawWaypointElement(i);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Add Waypoint", buttonStyle))
            {
                zigzagEffect.AddWaypoint();
                EditorUtility.SetDirty(zigzagEffect);
            }
            if (GUILayout.Button("🧹 Clear All", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("Clear Waypoints",
                    "Are you sure you want to clear all waypoints?", "Yes", "Cancel"))
                {
                    zigzagEffect.ClearWaypoints();
                    EditorUtility.SetDirty(zigzagEffect);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
    }
    private void DrawWaypointElement(int index)
    {
        SerializedProperty waypoint = waypointsProperty.GetArrayElementAtIndex(index);
        SerializedProperty localPos = waypoint.FindPropertyRelative("localPosition");
        SerializedProperty waitTime = waypoint.FindPropertyRelative("waitTime");
        SerializedProperty speedCurve = waypoint.FindPropertyRelative("speedCurve");
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(50));
        EditorGUI.BeginChangeCheck();
        Vector3 newPos = EditorGUILayout.Vector3Field("", localPos.vector3Value, GUILayout.Width(150));
        if (EditorGUI.EndChangeCheck())
        {
            localPos.vector3Value = newPos;
            EditorUtility.SetDirty(zigzagEffect);
        }
        EditorGUI.BeginChangeCheck();
        float newWaitTime = EditorGUILayout.FloatField(waitTime.floatValue, GUILayout.Width(80));
        if (EditorGUI.EndChangeCheck())
        {
            waitTime.floatValue = Mathf.Max(0, newWaitTime);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(54);
        if (GUILayout.Button("📍 Focus", GUILayout.Width(70), GUILayout.Height(25)))
        {
            FocusSceneViewOnWaypoint(index);
        }
        GUILayout.Space(5);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ Delete", GUILayout.Width(80), GUILayout.Height(25)))
        {
            zigzagEffect.RemoveWaypoint(index);
            EditorUtility.SetDirty(zigzagEffect);
            return;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel++;
        try
        {
            if (speedCurve != null && speedCurve.animationCurveValue != null)
            {
                EditorGUI.BeginChangeCheck();
                AnimationCurve newCurve = EditorGUILayout.CurveField(new GUIContent("Speed Curve"), speedCurve.animationCurveValue);
                if (EditorGUI.EndChangeCheck())
                {
                    speedCurve.animationCurveValue = newCurve;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Speed curve is null. Click 'Reset Curve' to fix.", MessageType.Warning);
                if (GUILayout.Button("Reset Curve"))
                {
                    speedCurve.animationCurveValue = AnimationCurve.Linear(0, 1, 1, 1);
                }
            }
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Curve editor error: {e.Message}", MessageType.Error);
            if (GUILayout.Button("Reset Curve"))
            {
                speedCurve.animationCurveValue = AnimationCurve.Linear(0, 1, 1, 1);
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    private void DrawGizmoSettings()
    {
        showGizmoSettings = EditorGUILayout.Foldout(showGizmoSettings, "🎨 Gizmo Settings", true);
        if (showGizmoSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(showGizmosProperty);
            if (showGizmosProperty.boolValue)
            {
                EditorGUILayout.PropertyField(waypointColorProperty);
                EditorGUILayout.PropertyField(pathColorProperty);
                EditorGUILayout.PropertyField(waypointSizeProperty);
            }
            EditorGUI.indentLevel--;
        }
    }
    private void DrawQuickActions()
    {
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Create Circle Path", buttonStyle))
        {
            CreateCirclePath();
        }
        if (GUILayout.Button("📐 Create Line Path", buttonStyle))
        {
            CreateLinePath();
        }
        if (GUILayout.Button("🔀 Create Random Path", buttonStyle))
        {
            CreateRandomPath();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy Waypoints", buttonStyle))
        {
            CopyWaypointsToClipboard();
        }
        if (GUILayout.Button("📄 Paste Waypoints", buttonStyle))
        {
            PasteWaypointsFromClipboard();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void FocusSceneViewOnWaypoint(int index)
    {
        if (index >= 0 && index < zigzagEffect.GetWaypoints().Length)
        {
            Vector3 worldPos = zigzagEffect.transform.TransformPoint(zigzagEffect.GetWaypoints()[index].localPosition);
            SceneView.lastActiveSceneView.LookAt(worldPos);
        }
    }
    private void CreateCirclePath()
    {
        int points = 8;
        float radius = 5f;
        WaypointData[] newWaypoints = new WaypointData[points];
        for (int i = 0; i < points; i++)
        {
            float angle = (float)i / points * 2 * Mathf.PI;
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            newWaypoints[i] = new WaypointData(pos);
            if (newWaypoints[i].speedCurve == null)
            {
                newWaypoints[i].speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
            }
        }
        zigzagEffect.SetWaypoints(newWaypoints);
        EditorUtility.SetDirty(zigzagEffect);
    }
    private void CreateLinePath()
    {
        int points = 5;
        float spacing = 5f;
        WaypointData[] newWaypoints = new WaypointData[points];
        for (int i = 0; i < points; i++)
        {
            Vector3 pos = new Vector3(0, 0, i * spacing);
            newWaypoints[i] = new WaypointData(pos);
        }
        zigzagEffect.SetWaypoints(newWaypoints);
        EditorUtility.SetDirty(zigzagEffect);
    }
    private void CreateRandomPath()
    {
        int points = Random.Range(4, 10);
        float range = 10f;
        WaypointData[] newWaypoints = new WaypointData[points];
        for (int i = 0; i < points; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-range, range),
                Random.Range(-2f, 5f),
                i * 3f + Random.Range(-2f, 2f)
            );
            newWaypoints[i] = new WaypointData(pos);
        }
        zigzagEffect.SetWaypoints(newWaypoints);
        EditorUtility.SetDirty(zigzagEffect);
    }
    private void CopyWaypointsToClipboard()
    {
        string waypointData = "";
        WaypointData[] waypoints = zigzagEffect.GetWaypoints();
        foreach (var waypoint in waypoints)
        {
            waypointData += $"{waypoint.localPosition.x},{waypoint.localPosition.y},{waypoint.localPosition.z},{waypoint.waitTime}\n";
        }
        EditorGUIUtility.systemCopyBuffer = waypointData;
        Debug.Log("Waypoints copied to clipboard!");
    }
    private void PasteWaypointsFromClipboard()
    {
        string clipboardText = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboardText)) return;
        string[] lines = clipboardText.Split('\n');
        List<WaypointData> newWaypoints = new List<WaypointData>();
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim())) continue;
            string[] values = line.Split(',');
            if (values.Length >= 3)
            {
                if (float.TryParse(values[0], out float x) &&
                    float.TryParse(values[1], out float y) &&
                    float.TryParse(values[2], out float z))
                {
                    Vector3 pos = new Vector3(x, y, z);
                    WaypointData waypoint = new WaypointData(pos);
                    if (values.Length >= 4 && float.TryParse(values[3], out float waitTime))
                    {
                        waypoint.waitTime = waitTime;
                    }
                    newWaypoints.Add(waypoint);
                }
            }
        }
        if (newWaypoints.Count > 0)
        {
            zigzagEffect.SetWaypoints(newWaypoints.ToArray());
            EditorUtility.SetDirty(zigzagEffect);
            Debug.Log($"Pasted {newWaypoints.Count} waypoints from clipboard!");
        }
    }
    private void OnSceneGUI()
    {
        if (!zigzagEffect.showGizmos) return;
        WaypointData[] waypoints = zigzagEffect.GetWaypoints();
        if (waypoints == null) return;
        Handles.color = zigzagEffect.waypointColor;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 worldPos = zigzagEffect.transform.TransformPoint(waypoints[i].localPosition);
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(zigzagEffect, "Move Waypoint");
                waypoints[i].localPosition = zigzagEffect.transform.InverseTransformPoint(newWorldPos);
                EditorUtility.SetDirty(zigzagEffect);
            }
            Handles.Label(worldPos + Vector3.up * 2f,
                         $"Waypoint {i}\nWait: {waypoints[i].waitTime}s");
        }
    }
}
#endif