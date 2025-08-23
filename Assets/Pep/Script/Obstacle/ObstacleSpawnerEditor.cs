using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObstacleSpawner))]
public class ObstacleSpawnerEditor : Editor
{
    private ObstacleSpawner spawner;
    private bool showPreview = true;

    private void OnEnable()
    {
        spawner = (ObstacleSpawner)target;
    }

    public override void OnInspectorGUI()
    {

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Obstacle Spawner Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        SerializedProperty endPointProp = serializedObject.FindProperty("endPoint");
        EditorGUILayout.PropertyField(endPointProp, new GUIContent("End Point", "Target point where obstacles will move towards"));

        if (GUILayout.Button("Create", GUILayout.Width(60)))
        {
            CreateEndPoint();
        }
        EditorGUILayout.EndHorizontal();

        if (spawner.EndPoint != null)
        {
            float distance = Vector3.Distance(spawner.transform.position, spawner.EndPoint.position);
            EditorGUILayout.HelpBox($"Distance to endpoint: {distance:F2} units", MessageType.Info);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Timing Settings", EditorStyles.boldLabel);
        SerializedProperty spawnIntervalProp = serializedObject.FindProperty("spawnInterval");
        EditorGUILayout.PropertyField(spawnIntervalProp, new GUIContent("Spawn Interval", "Time between spawns in seconds"));

        SerializedProperty lifetimeProp = serializedObject.FindProperty("obstacleLifetime");
        EditorGUILayout.PropertyField(lifetimeProp, new GUIContent("Obstacle Lifetime", "How long obstacles exist before being destroyed"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
        SerializedProperty speedProp = serializedObject.FindProperty("speed");
        EditorGUILayout.PropertyField(speedProp, new GUIContent("Speed", "Movement speed of spawned obstacles"));

        if (spawner.EndPoint != null && spawner.Speed > 0)
        {
            float distance = Vector3.Distance(spawner.transform.position, spawner.EndPoint.position);
            float travelTime = distance / spawner.Speed;
            EditorGUILayout.HelpBox($"Travel time to endpoint: {travelTime:F2} seconds", MessageType.Info);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Obstacle Settings", EditorStyles.boldLabel);
        SerializedProperty prefabProp = serializedObject.FindProperty("obstaclePrefab");
        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Obstacle Prefab", "Prefab to spawn (must have ObstacleBase component)"));

        if (spawner.gameObject.scene.IsValid())
        {
            ValidateObstaclePrefab();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
        SerializedProperty showGizmosProp = serializedObject.FindProperty("showGizmos");
        EditorGUILayout.PropertyField(showGizmosProp, new GUIContent("Show Gizmos", "Show spawn direction in scene view"));

        if (spawner.gameObject.scene.IsValid())
        {
            SerializedProperty gizmoColorProp = serializedObject.FindProperty("gizmoColor");
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color"));
        }

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Start Spawning"))
            {
                spawner.StartSpawning();
            }

            if (GUILayout.Button("Stop Spawning"))
            {
                spawner.StopSpawning();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Spawn One Now"))
            {

                spawner.GetType().GetMethod("SpawnObstacle",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(spawner, null);
            }
        }

        if (serializedObject.ApplyModifiedProperties())
        {

            EditorUtility.SetDirty(spawner);
        }
    }

    private void CreateEndPoint()
    {

        GameObject endPoint = new GameObject($"{spawner.gameObject.name}_EndPoint");
        endPoint.transform.position = spawner.transform.position + spawner.transform.forward * 5f;

        Undo.RegisterCreatedObjectUndo(endPoint, "Create End Point");

        spawner.EndPoint = endPoint.transform;

        EditorUtility.SetDirty(spawner);

        Selection.activeGameObject = endPoint;
    }

    private void ValidateObstaclePrefab()
    {
        SerializedProperty prefabProp = serializedObject.FindProperty("obstaclePrefab");
        GameObject prefab = prefabProp.objectReferenceValue as GameObject;

        if (prefab != null)
        {
            ObstacleBase obstacleBase = prefab.GetComponent<ObstacleBase>();
            if (obstacleBase == null)
            {
                EditorGUILayout.HelpBox("Obstacle prefab must have an ObstacleBase component!", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ Valid obstacle prefab with {obstacleBase.GetType().Name} component", MessageType.Info);
            }

            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                EditorGUILayout.HelpBox("Obstacle prefab should have a Rigidbody component!", MessageType.Warning);
            }

            Collider col = prefab.GetComponent<Collider>();
            if (col == null)
            {
                EditorGUILayout.HelpBox("Obstacle prefab should have a Collider component!", MessageType.Warning);
            }
        }
    }

    private void OnSceneGUI()
    {
        if (spawner == null || spawner.EndPoint == null) return;

        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(spawner.EndPoint.position, spawner.EndPoint.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spawner.EndPoint, "Move End Point");
            spawner.EndPoint.position = newPos;
            EditorUtility.SetDirty(spawner.EndPoint);
        }

        Handles.Label(spawner.transform.position + Vector3.up * 0.5f, "Spawner", EditorStyles.boldLabel);
        Handles.Label(spawner.EndPoint.position + Vector3.up * 0.5f, "End Point", EditorStyles.boldLabel);

        Vector3 direction = (spawner.EndPoint.position - spawner.transform.position).normalized;
        float distance = Vector3.Distance(spawner.transform.position, spawner.EndPoint.position);

        if (distance > 0 && spawner.Speed > 0)
        {
            float travelTime = distance / spawner.Speed;
            Vector3 midPoint = Vector3.Lerp(spawner.transform.position, spawner.EndPoint.position, 0.5f);
            Handles.Label(midPoint, $"Travel Time: {travelTime:F1}s\nDistance: {distance:F1}m\nSpeed: {spawner.Speed:F1}m/s");
        }
    }
}