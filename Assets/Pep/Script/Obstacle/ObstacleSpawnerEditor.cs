//using UnityEditor;
//using UnityEngine;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//[CustomEditor(typeof(ObstacleSpawner))]
//public class ObstacleSpawnerEditor : Editor
//{
//    private static Dictionary<string, List<ObstacleBase>> obstacleTypeCache = new Dictionary<string, List<ObstacleBase>>();
//    private static bool cacheNeedsRefresh = true;

//    private void OnSceneGUI()
//    {
//        ObstacleSpawner spawner = (ObstacleSpawner)target;

//        if (!spawner.useEndPoint) return;

//        EditorGUI.BeginChangeCheck();
//        Vector3 worldEndPoint = spawner.WorldEndPoint;
//        Vector3 newWorldEndPoint = Handles.PositionHandle(worldEndPoint, Quaternion.identity);
//        Handles.Label(newWorldEndPoint + Vector3.up * 0.3f, "Target Point");

//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Move Target Point");
//            spawner.WorldEndPoint = newWorldEndPoint;
//            EditorUtility.SetDirty(spawner);
//        }

//        Vector3 startPos = spawner.SpawnPoint;
//        Vector3 endPos = spawner.WorldEndPoint;
//        float distance = Vector3.Distance(startPos, endPos);

//        Handles.color = Color.cyan;
//        Handles.DrawDottedLine(startPos, endPos, 5f);
//        Handles.Label(Vector3.Lerp(startPos, endPos, 0.5f), $"Range: {distance:F1}m\nDirection: {spawner.LaunchDirection}");
//    }

//    public override void OnInspectorGUI()
//    {
//        ObstacleSpawner spawner = (ObstacleSpawner)target;

//        EditorGUILayout.LabelField("🎯 Turret Obstacle Spawner", EditorStyles.boldLabel);
//        EditorGUILayout.HelpBox("Auto-discovers all obstacle types in your project!", MessageType.Info);

//        EditorGUILayout.Space();

//        // Obstacle Type Selection
//        EditorGUILayout.LabelField("Obstacle Selection", EditorStyles.boldLabel);

//        // Refresh cache button
//        EditorGUILayout.BeginHorizontal();
//        if (GUILayout.Button("🔄 Refresh Obstacle Types", GUILayout.Width(150)))
//        {
//            RefreshObstacleCache();
//        }

//        // Show cache status
//        EditorGUI.BeginDisabledGroup(true);
//        EditorGUILayout.TextField($"Found: {GetTotalObstacleCount()} obstacles", GUILayout.ExpandWidth(true));
//        EditorGUI.EndDisabledGroup();
//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.Space();

//        // Display obstacle types with dropdowns
//        DrawObstacleTypeSelection(spawner);

//        // Show current selection info
//        if (spawner.ObstaclePrefab != null)
//        {
//            EditorGUILayout.BeginVertical("Box");
//            EditorGUILayout.LabelField("Current Selection:", EditorStyles.miniLabel);
//            EditorGUI.BeginDisabledGroup(true);
//            EditorGUILayout.TextField("Type", spawner.ObstacleTypeName);
//            EditorGUILayout.TextField("Prefab Name", spawner.ObstaclePrefab.name);
//            EditorGUILayout.TextField("Path", AssetDatabase.GetAssetPath(spawner.ObstaclePrefab));
//            EditorGUI.EndDisabledGroup();
//            EditorGUILayout.EndVertical();
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("⚠️ No obstacle selected. Choose from available types above.", MessageType.Warning);
//        }

//        EditorGUILayout.Space();

//        EditorGUILayout.LabelField("Turret Settings", EditorStyles.boldLabel);
//        EditorGUI.BeginChangeCheck();
//        float newSpawnInterval = EditorGUILayout.FloatField("Fire Rate (s)", spawner.spawnInterval);
//        float newObstacleLifetime = EditorGUILayout.FloatField("Projectile Lifetime (s)", spawner.obstacleLifetime);
//        float newSpeed = EditorGUILayout.FloatField("Launch Speed", spawner.speed);

//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Change Turret Settings");
//            spawner.spawnInterval = Mathf.Max(0.1f, newSpawnInterval);
//            spawner.obstacleLifetime = Mathf.Max(0.1f, newObstacleLifetime);
//            spawner.speed = Mathf.Max(0.1f, newSpeed);
//            EditorUtility.SetDirty(spawner);
//        }

//        EditorGUILayout.Space();

//        EditorGUILayout.LabelField("Auto Setup Options", EditorStyles.boldLabel);
//        EditorGUI.BeginChangeCheck();
//        bool autoRb = EditorGUILayout.Toggle("Auto Add Rigidbody", spawner.autoAddRigidbody);
//        bool autoCol = EditorGUILayout.Toggle("Auto Add Collider", spawner.autoAddCollider);
//        PhysicsMaterial physMat = (PhysicsMaterial)EditorGUILayout.ObjectField("Physic Material", spawner.physicMaterial, typeof(PhysicsMaterial), false);
//        LayerMask obsLayer = EditorGUILayoutExtensions.LayerMaskField("Obstacle Layer", spawner.obstacleLayer);

//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Change Auto Setup Options");
//            spawner.autoAddRigidbody = autoRb;
//            spawner.autoAddCollider = autoCol;
//            spawner.physicMaterial = physMat;
//            spawner.obstacleLayer = obsLayer;
//            EditorUtility.SetDirty(spawner);
//        }

//        EditorGUI.BeginDisabledGroup(spawner.ObstaclePrefab == null);
//        if (GUILayout.Button("🔧 Setup Obstacle Prefab", GUILayout.Height(30)))
//        {
//            ObstacleBase result = spawner.SetupObstaclePrefab();
//            if (result != null)
//                EditorUtility.DisplayDialog("Setup Complete!", $"Successfully setup {spawner.ObstacleTypeName} obstacle!", "OK");
//        }
//        EditorGUI.EndDisabledGroup();

//        if (spawner.ObstaclePrefab == null)
//            EditorGUILayout.HelpBox("⚠️ Please select an obstacle type first!", MessageType.Warning);

//        EditorGUILayout.Space();

//        // Target Settings (keeping your existing target code)
//        EditorGUILayout.LabelField("Target Settings", EditorStyles.boldLabel);
//        EditorGUI.BeginChangeCheck();
//        bool newUseEndPoint = EditorGUILayout.Toggle("Use Target Point", spawner.useEndPoint);
//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Toggle Use Target Point");
//            spawner.useEndPoint = newUseEndPoint;
//            EditorUtility.SetDirty(spawner);
//        }

//        if (spawner.useEndPoint)
//        {
//            EditorGUILayout.HelpBox("💡 Drag the yellow sphere in Scene view to adjust the target point!", MessageType.Info);
//            Vector3 worldEndPoint = spawner.WorldEndPoint;
//            EditorGUI.BeginChangeCheck();
//            Vector3 newWorldEndPoint = EditorGUILayout.Vector3Field("Target Point (World)", worldEndPoint);
//            if (EditorGUI.EndChangeCheck())
//            {
//                Undo.RecordObject(spawner, "Change Target Point");
//                spawner.WorldEndPoint = newWorldEndPoint;
//                EditorUtility.SetDirty(spawner);
//            }

//            Vector3 startPos = spawner.SpawnPoint;
//            float distance = Vector3.Distance(startPos, worldEndPoint);
//            Vector3 direction = spawner.LaunchDirection;

//            EditorGUILayout.Space();
//            EditorGUILayout.LabelField("📊 Turret Data", EditorStyles.miniLabel);
//            EditorGUI.BeginDisabledGroup(true);
//            EditorGUILayout.Vector3Field("Turret Position", startPos);
//            EditorGUILayout.FloatField("Target Distance", distance);
//            EditorGUILayout.Vector3Field("Fire Direction", direction);
//            EditorGUI.EndDisabledGroup();

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("Reset to Forward"))
//            {
//                Undo.RecordObject(spawner, "Reset Target Point");
//                spawner.WorldEndPoint = spawner.transform.position + spawner.transform.forward * 5f;
//                EditorUtility.SetDirty(spawner);
//            }
//            if (GUILayout.Button("Set Range 10m"))
//            {
//                Undo.RecordObject(spawner, "Set Target Distance");
//                Vector3 currentDir = spawner.LaunchDirection;
//                spawner.WorldEndPoint = spawner.SpawnPoint + currentDir * 10f;
//                EditorUtility.SetDirty(spawner);
//            }
//            EditorGUILayout.EndHorizontal();
//        }

//        EditorGUILayout.Space();

//        // Control Section (keeping your existing control code)
//        EditorGUILayout.LabelField("Turret Control", EditorStyles.boldLabel);
//        EditorGUI.BeginChangeCheck();
//        spawner.spawnOnStart = EditorGUILayout.Toggle("Fire On Start", spawner.spawnOnStart);
//        spawner.autoSpawn = EditorGUILayout.Toggle("Auto Fire", spawner.autoSpawn);
//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Change Turret Control");
//            EditorUtility.SetDirty(spawner);
//        }

//        EditorGUILayout.Space();

//        EditorGUILayout.BeginVertical("Box");
//        EditorGUI.BeginDisabledGroup(spawner.ObstaclePrefab == null || !Application.isPlaying);

//        EditorGUILayout.BeginHorizontal();
//        if (GUILayout.Button("🔥 Fire Once", GUILayout.Height(35)))
//            spawner.SpawnObstacle();

//        if (spawner.autoSpawn)
//        {
//            if (GUILayout.Button("⏸️ Stop Auto Fire", GUILayout.Height(35)))
//                spawner.StopSpawning();
//        }
//        else
//        {
//            if (GUILayout.Button("▶️ Start Auto Fire", GUILayout.Height(35)))
//                spawner.StartSpawning();
//        }
//        EditorGUILayout.EndHorizontal();

//        EditorGUI.EndDisabledGroup();

//        if (GUILayout.Button("🔄 Reset Turret"))
//        {
//            spawner.ResetSpawner();
//            EditorUtility.SetDirty(spawner);
//        }
//        EditorGUILayout.EndVertical();

//        if (!Application.isPlaying)
//            EditorGUILayout.HelpBox("🎮 Enter Play Mode to test turret firing!", MessageType.Info);

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("📋 System Status", EditorStyles.boldLabel);

//        if (spawner.ObstaclePrefab == null)
//        {
//            EditorGUILayout.HelpBox("⚠️ No obstacle selected. Please choose an obstacle type from the dropdowns above.", MessageType.Error);
//        }
//        else
//        {
//            EditorGUILayout.HelpBox($"✅ Turret ready to fire {spawner.ObstacleTypeName} obstacles\n" +
//                                  $"💥 Fire Rate: {spawner.spawnInterval:F1}s intervals\n" +
//                                  $"⏱️ Projectile Lifetime: {spawner.obstacleLifetime:F1}s\n" +
//                                  $"🚀 Launch Speed: {spawner.speed:F1} units/s", MessageType.Info);
//        }

//        if (GUI.changed)
//            EditorUtility.SetDirty(spawner);
//    }

//    private void DrawObstacleTypeSelection(ObstacleSpawner spawner)
//    {
//        if (cacheNeedsRefresh)
//            RefreshObstacleCache();

//        if (obstacleTypeCache.Count == 0)
//        {
//            EditorGUILayout.HelpBox("🔍 No obstacle prefabs found! Make sure you have prefabs with ObstacleBase components in your project.", MessageType.Warning);
//            return;
//        }

//        EditorGUILayout.BeginVertical("Box");
//        EditorGUILayout.LabelField("Available Obstacle Types:", EditorStyles.boldLabel);

//        foreach (var kvp in obstacleTypeCache.OrderBy(x => x.Key))
//        {
//            string typeName = kvp.Key;
//            List<ObstacleBase> prefabsOfType = kvp.Value;

//            if (prefabsOfType.Count == 0) continue;

//            EditorGUILayout.BeginHorizontal();

//            // Type label with count
//            EditorGUILayout.LabelField($"🎯 {typeName} ({prefabsOfType.Count})", GUILayout.Width(150));

//            // Dropdown for prefabs of this type
//            string[] prefabNames = prefabsOfType.Select(p => p ? p.name : "Missing").ToArray();
//            int currentIndex = -1;

//            if (spawner.ObstaclePrefab != null)
//            {
//                currentIndex = prefabsOfType.FindIndex(p => p == spawner.ObstaclePrefab);
//            }

//            EditorGUI.BeginChangeCheck();
//            int newIndex = EditorGUILayout.Popup(currentIndex, prefabNames);
//            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < prefabsOfType.Count)
//            {
//                Undo.RecordObject(spawner, "Change Obstacle Prefab");
//                spawner.ObstaclePrefab = prefabsOfType[newIndex];
//                EditorUtility.SetDirty(spawner);
//            }

//            // Quick select button
//            if (prefabsOfType.Count == 1 && GUILayout.Button("Select", GUILayout.Width(60)))
//            {
//                Undo.RecordObject(spawner, "Select Obstacle Prefab");
//                spawner.ObstaclePrefab = prefabsOfType[0];
//                EditorUtility.SetDirty(spawner);
//            }

//            EditorGUILayout.EndHorizontal();
//        }

//        EditorGUILayout.EndVertical();

//        // Manual assignment fallback
//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Manual Assignment:", EditorStyles.miniLabel);
//        EditorGUI.BeginChangeCheck();
//        ObstacleBase manualPrefab = (ObstacleBase)EditorGUILayout.ObjectField("Override Prefab", spawner.ObstaclePrefab, typeof(ObstacleBase), false);
//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(spawner, "Manual Change Obstacle Prefab");
//            spawner.ObstaclePrefab = manualPrefab;
//            EditorUtility.SetDirty(spawner);
//        }
//    }

//    private void RefreshObstacleCache()
//    {
//        obstacleTypeCache.Clear();

//        // Find all prefabs with ObstacleBase components
//        string[] guids = AssetDatabase.FindAssets("t:Prefab");

//        foreach (string guid in guids)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guid);
//            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

//            if (prefab != null)
//            {
//                ObstacleBase obstacleComponent = prefab.GetComponent<ObstacleBase>();
//                if (obstacleComponent != null)
//                {
//                    string typeName = obstacleComponent.GetType().Name;

//                    if (!obstacleTypeCache.ContainsKey(typeName))
//                    {
//                        obstacleTypeCache[typeName] = new List<ObstacleBase>();
//                    }

//                    obstacleTypeCache[typeName].Add(obstacleComponent);
//                }
//            }
//        }

//        cacheNeedsRefresh = false;

//        Debug.Log($"🔄 Obstacle Cache Refreshed: Found {GetTotalObstacleCount()} obstacle prefabs across {obstacleTypeCache.Count} types");
//    }

//    private int GetTotalObstacleCount()
//    {
//        return obstacleTypeCache.Values.Sum(list => list.Count);
//    }

//    private void OnEnable()
//    {
//        // Refresh cache when editor is enabled
//        cacheNeedsRefresh = true;
//    }

//    private void OnDisable()
//    {
//        // Mark cache for refresh when editor is disabled
//        cacheNeedsRefresh = true;
//    }
//}

//public static class EditorGUILayoutExtensions
//{
//    public static LayerMask LayerMaskField(string label, LayerMask layerMask)
//    {
//        var layers = UnityEditorInternal.InternalEditorUtility.layers;
//        var layerNumbers = new System.Collections.Generic.List<int>();
//        var layerNames = new System.Collections.Generic.List<string>();

//        for (int i = 0; i < layers.Length; i++)
//        {
//            int layer = LayerMask.NameToLayer(layers[i]);
//            if (layer >= 0)
//            {
//                layerNumbers.Add(layer);
//                layerNames.Add(layers[i]);
//            }
//        }

//        int maskWithoutEmpty = 0;
//        for (int i = 0; i < layerNumbers.Count; i++)
//        {
//            if (((1 << layerNumbers[i]) & layerMask.value) > 0)
//                maskWithoutEmpty |= (1 << i);
//        }

//        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layerNames.ToArray());

//        int mask = 0;
//        for (int i = 0; i < layerNumbers.Count; i++)
//        {
//            if ((maskWithoutEmpty & (1 << i)) > 0)
//                mask |= (1 << layerNumbers[i]);
//        }

//        return mask;
//    }
//}