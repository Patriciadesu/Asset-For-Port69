using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomEditor(typeof(ObstacleSpawner))]
public class ObstacleSpawnerEditor : Editor
{
    private ObstacleSpawner spawner;

    // Serialized
    private SerializedProperty endPointProp;
    private SerializedProperty spawnIntervalProp;
    private SerializedProperty speedProp;
    private SerializedProperty lifetimeProp;
    private SerializedProperty obstaclePrefabProp;

    // Cache for inline component editor
    private Editor obstacleBaseEditor;

    // Derived types cache for dropdown
    private Type[]  obstacleTypes;
    private string[] obstacleTypeNames;
    private int selectionIndex = 0; // 0 = placeholder

    private void OnEnable()
    {
        spawner = (ObstacleSpawner)target;

        endPointProp        = serializedObject.FindProperty("endPoint");
        spawnIntervalProp   = serializedObject.FindProperty("spawnInterval");
        speedProp           = serializedObject.FindProperty("speed");
        lifetimeProp        = serializedObject.FindProperty("obstacleLifetime");
        obstaclePrefabProp  = serializedObject.FindProperty("obstaclePrefab");

        obstacleTypes = TypeCache.GetTypesDerivedFrom<ObstacleBase>()
            .Where(t => !t.IsAbstract && typeof(ObstacleBase).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        obstacleTypeNames = new[] { "Select Obstacle Behavior…" }
            .Concat(obstacleTypes.Select(t => t.Name))
            .ToArray();
    }

    private void OnDisable()
    {
        if (obstacleBaseEditor != null)
        {
            DestroyImmediate(obstacleBaseEditor);
            obstacleBaseEditor = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSpawnerSection();
        EditorGUILayout.Space(6);

        DrawBulletSection(); // dropdown add/replace + inline inspector + collider controls
        EditorGUILayout.Space(8);

        if (serializedObject.ApplyModifiedProperties())
            EditorUtility.SetDirty(spawner);
    }

    private void DrawSpawnerSection()
    {
        EditorGUILayout.LabelField("Spawner Settings", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(endPointProp, new GUIContent("End Point"));
                using (new EditorGUI.DisabledScope(!spawner.gameObject.scene.IsValid()))
                {
                    if (GUILayout.Button("Create", GUILayout.Width(70)))
                        CreateEndPoint();
                }
            }

            EditorGUILayout.PropertyField(spawnIntervalProp, new GUIContent("Spawn Interval (s)"));
            EditorGUILayout.PropertyField(speedProp,         new GUIContent("Speed (m/s)"));
            EditorGUILayout.PropertyField(lifetimeProp,      new GUIContent("Obstacle Lifetime (s)"));
            EditorGUILayout.PropertyField(obstaclePrefabProp, new GUIContent("Bullet Prefab"));
        }
    }

    private void DrawBulletSection()
    {
        EditorGUILayout.LabelField("Bullet Behavior", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            var currentRef = obstaclePrefabProp.objectReferenceValue as GameObject;
            if (currentRef == null)
            {
                EditorGUILayout.HelpBox("Assign a Bullet Prefab to configure behavior.", MessageType.Info);
                return;
            }

            // Normalize to prefab asset when available so edits persist and references stay stable.
            var prefabAsset = GetPrefabAssetFromReference(currentRef);
            if (prefabAsset == null)
            {
                // Scene object; allow direct edit (non-persistent to assets)
                EditorGUILayout.HelpBox("Bullet Prefab is a scene object; behavior edits won’t persist to assets.", MessageType.Warning);
                ValidateObstaclePrefab(currentRef);
                DrawBehaviorDropdownAndInspector_OnObject(currentRef);
                DrawColliderControlsOnObject(currentRef);      // ← isTrigger control (scene object)
                return;
            }

            if (prefabAsset != currentRef)
            {
                obstaclePrefabProp.objectReferenceValue = prefabAsset;
                serializedObject.ApplyModifiedProperties();
            }

            ValidateObstaclePrefab(prefabAsset);
            DrawBehaviorDropdownAndInspector_OnPrefabAsset(prefabAsset);
            DrawColliderControlsOnPrefabAsset(prefabAsset);    // ← isTrigger control (prefab asset)
        }
    }

    // ——— Dropdown + Inspector: Scene Object ———
    private void DrawBehaviorDropdownAndInspector_OnObject(GameObject go)
    {
        var baseComp = go.GetComponent<ObstacleBase>();
        selectionIndex = GetTypeIndex(baseComp?.GetType());

        // Dropdown (add/replace)
        EditorGUI.BeginChangeCheck();
        selectionIndex = EditorGUILayout.Popup(new GUIContent("Obstacle Base"), selectionIndex, obstacleTypeNames);
        if (EditorGUI.EndChangeCheck() && selectionIndex >= 0)
        {
            if (selectionIndex > 0)
            {
                var chosenType = obstacleTypes[selectionIndex - 1];
                ReplaceBehaviorOnObject(go, chosenType);
                EnsureRigidbody(go);
                ResetCachedEditor();
                Repaint();
            }
        }

        // Inline inspector for current behavior
        baseComp = go.GetComponent<ObstacleBase>();
        if (baseComp != null)
        {
            EditorGUILayout.LabelField("Current Behavior", baseComp.GetType().Name);
            EditorGUI.indentLevel++;
            Editor.CreateCachedEditor(baseComp, null, ref obstacleBaseEditor);
            obstacleBaseEditor?.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("No ObstacleBase on the object.", MessageType.Info);
        }
    }

    // ——— Dropdown + Inspector: Prefab Asset ———
    private void DrawBehaviorDropdownAndInspector_OnPrefabAsset(GameObject prefabAsset)
    {
        var baseComp = prefabAsset.GetComponent<ObstacleBase>();
        selectionIndex = GetTypeIndex(baseComp?.GetType());

        // Dropdown (add/replace)
        EditorGUI.BeginChangeCheck();
        selectionIndex = EditorGUILayout.Popup(new GUIContent("Obstacle Base"), selectionIndex, obstacleTypeNames);
        if (EditorGUI.EndChangeCheck() && selectionIndex >= 0)
        {
            if (selectionIndex > 0)
            {
                var chosenType = obstacleTypes[selectionIndex - 1];
                ReplaceBehaviorOnPrefabAsset(prefabAsset, chosenType, alsoEnsureRigidbody: true);

                // Re-assign same asset to refresh
                var path = AssetDatabase.GetAssetPath(prefabAsset);
                var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                obstaclePrefabProp.objectReferenceValue = reloaded;
                serializedObject.ApplyModifiedProperties();

                ResetCachedEditor();
                Repaint();
                GUIUtility.ExitGUI();
                return;
            }
        }

        // Inline inspector for current behavior
        baseComp = (obstaclePrefabProp.objectReferenceValue as GameObject)?.GetComponent<ObstacleBase>();
        if (baseComp != null)
        {
            EditorGUILayout.LabelField("Current Behavior", baseComp.GetType().Name);
            EditorGUI.indentLevel++;
            Editor.CreateCachedEditor(baseComp, null, ref obstacleBaseEditor);
            obstacleBaseEditor?.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("No ObstacleBase on the prefab asset.", MessageType.Info);
        }
    }

    // ——— Collider Settings (Scene Object) ———
    private void DrawColliderControlsOnObject(GameObject go)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            var colliders = go.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                EditorGUILayout.HelpBox("No Collider on bullet.", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add BoxCollider"))     Undo.AddComponent<BoxCollider>(go);
                    if (GUILayout.Button("Add SphereCollider"))  Undo.AddComponent<SphereCollider>(go);
                    if (GUILayout.Button("Add CapsuleCollider")) Undo.AddComponent<CapsuleCollider>(go);
                }
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                EditorGUI.BeginChangeCheck();
                bool isTrig = EditorGUILayout.ToggleLeft($"{c.GetType().Name} (root) — Is Trigger", c.isTrigger);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(c, "Toggle Is Trigger");
                    c.isTrigger = isTrig;
                    EditorUtility.SetDirty(c);
                }
            }
        }
    }

    // ——— Collider Settings (Prefab Asset) ———
    private void DrawColliderControlsOnPrefabAsset(GameObject prefabAsset)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            // Read current state from the asset root
            var colliders = prefabAsset.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                EditorGUILayout.HelpBox("No Collider on bullet prefab.", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add BoxCollider"))     { AddColliderToPrefabAsset<BoxCollider>(prefabAsset);  RefreshAssetRef(prefabAsset); }
                    if (GUILayout.Button("Add SphereCollider"))  { AddColliderToPrefabAsset<SphereCollider>(prefabAsset); RefreshAssetRef(prefabAsset); }
                    if (GUILayout.Button("Add CapsuleCollider")) { AddColliderToPrefabAsset<CapsuleCollider>(prefabAsset); RefreshAssetRef(prefabAsset); }
                }
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                bool newValue = EditorGUILayout.ToggleLeft($"{c.GetType().Name} (root) — Is Trigger", c.isTrigger);
                if (newValue != c.isTrigger)
                {
                    SetColliderIsTriggerOnPrefabAsset(prefabAsset, colliderIndex: i, newIsTrigger: newValue);
                    RefreshAssetRef(prefabAsset);
                    Repaint();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void DrawRuntimeControls()
    {
        if (!Application.isPlaying) return;

        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Start Spawning")) spawner.StartSpawning();
            if (GUILayout.Button("Stop Spawning"))  spawner.StopSpawning();
        }

        if (GUILayout.Button("Spawn One Now"))
        {
            var m = typeof(ObstacleSpawner).GetMethod(
                "SpawnObstacle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (m != null) m.Invoke(spawner, null);
        }
    }

    private void CreateEndPoint()
    {
        var go = new GameObject($"{spawner.gameObject.name}_EndPoint");
        Undo.RegisterCreatedObjectUndo(go, "Create End Point");
        go.transform.position = spawner.transform.position + spawner.transform.forward * 5f;

        endPointProp.objectReferenceValue = go.transform;
        serializedObject.ApplyModifiedProperties();

        Selection.activeGameObject = go;
        EditorUtility.SetDirty(spawner);
    }

    // ---------- Prefab / Component helpers ----------

    private static GameObject GetPrefabAssetFromReference(GameObject reference)
    {
        var assetPath = AssetDatabase.GetAssetPath(reference);
        if (!string.IsNullOrEmpty(assetPath))
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (PrefabUtility.IsPartOfPrefabInstance(reference))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(reference);
            var sourcePath = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(sourcePath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        }
        return null;
    }

    private static void EnsureRigidbody(GameObject go)
    {
        if (go == null) return;
        if (!go.TryGetComponent<Rigidbody>(out _))
            Undo.AddComponent<Rigidbody>(go);
    }

    private static void ReplaceBehaviorOnObject(GameObject go, Type chosenType)
    {
        if (go == null || chosenType == null) return;

        var all = go.GetComponents<ObstacleBase>();
        foreach (var comp in all)
            Undo.DestroyObjectImmediate(comp);

        Undo.AddComponent(go, chosenType);
    }

    private static void ReplaceBehaviorOnPrefabAsset(GameObject prefabAsset, Type chosenType, bool alsoEnsureRigidbody)
    {
        var path = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(path)) return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var all = root.GetComponents<ObstacleBase>();
            foreach (var comp in all)
                UnityEngine.Object.DestroyImmediate(comp, allowDestroyingAssets: true);

            if (root.GetComponent(chosenType) == null)
                root.AddComponent(chosenType);

            if (alsoEnsureRigidbody && root.GetComponent<Rigidbody>() == null)
                root.AddComponent<Rigidbody>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.ImportAsset(path);
        AssetDatabase.SaveAssets();
    }

    private static void AddColliderToPrefabAsset<T>(GameObject prefabAsset) where T : Collider
    {
        var path = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(path)) return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<T>() == null)
                root.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.ImportAsset(path);
        AssetDatabase.SaveAssets();
    }

    private static void SetColliderIsTriggerOnPrefabAsset(GameObject prefabAsset, int colliderIndex, bool newIsTrigger)
    {
        var path = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(path)) return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var colliders = root.GetComponents<Collider>();
            if (colliders != null && colliderIndex >= 0 && colliderIndex < colliders.Length)
            {
                colliders[colliderIndex].isTrigger = newIsTrigger;
                EditorUtility.SetDirty(colliders[colliderIndex]);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.ImportAsset(path);
        AssetDatabase.SaveAssets();
    }

    private int GetTypeIndex(Type currentType)
    {
        if (currentType == null) return 0;
        var i = Array.FindIndex(obstacleTypes, t => t == currentType);
        return (i >= 0) ? (i + 1) : 0; // +1 because 0 is "Select…"
    }

    private void ResetCachedEditor()
    {
        if (obstacleBaseEditor != null)
        {
            DestroyImmediate(obstacleBaseEditor);
            obstacleBaseEditor = null;
        }
    }

    private void RefreshAssetRef(GameObject prefabAsset)
    {
        var path = AssetDatabase.GetAssetPath(prefabAsset);
        var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        obstaclePrefabProp.objectReferenceValue = reloaded;
        serializedObject.ApplyModifiedProperties();
    }

    // Simple validation helper (shows hints and status)
    private static void ValidateObstaclePrefab(GameObject prefabLike)
    {
        if (prefabLike == null) return;

        bool hasBase      = prefabLike.GetComponent<ObstacleBase>() != null;
        bool hasRigidbody = prefabLike.GetComponent<Rigidbody>()    != null;
        bool hasCollider  = prefabLike.GetComponent<Collider>()     != null;

        if (!hasBase)
            EditorGUILayout.HelpBox("No ObstacleBase found. Choose a behavior from the dropdown below.", MessageType.Warning);

        if (!hasRigidbody)
            EditorGUILayout.HelpBox("A Rigidbody will be auto-added when you add/replace a behavior.", MessageType.None);

        if (!hasCollider)
            EditorGUILayout.HelpBox("No Collider found. You can add one below to enable trigger detection.", MessageType.None);
    }

    // ——— Scene GUI ———
    private void OnSceneGUI()
    {
        if (spawner == null) return;

        var end = spawner.EndPoint; // if not exposed, read via endPointProp.objectReferenceValue
        if (end == null) return;

        EditorGUI.BeginChangeCheck();
        var newPos = Handles.PositionHandle(end.position, end.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(end, "Move End Point");
            end.position = newPos;
            EditorUtility.SetDirty(end);
        }

        Handles.Label(spawner.transform.position + Vector3.up * 0.4f, "Spawner", EditorStyles.boldLabel);
        Handles.Label(end.position + Vector3.up * 0.4f, "End Point", EditorStyles.boldLabel);

        var distance = Vector3.Distance(spawner.transform.position, end.position);
        if (distance > 0f && spawner.Speed > 0f)
        {
            var travelTime = distance / spawner.Speed;
            var mid = Vector3.Lerp(spawner.transform.position, end.position, 0.5f);
            Handles.Label(mid, $"Distance: {distance:F1} m\nSpeed: {spawner.Speed:F1} m/s\nTravel Time: {travelTime:F1} s");
        }
    }
}
