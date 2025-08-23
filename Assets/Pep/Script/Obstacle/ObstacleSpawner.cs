//using UnityEngine;
//using System.Collections;
//using System;

//public class ObstacleSpawner : MonoBehaviour
//{
//    [Header("Obstacle Setup")]
//    [SerializeField] private GameObject basePrefab;
//    [SerializeField] private string selectedObstacleType = "ObstacleStraight";

//    [Header("Spawn Settings")]
//    [SerializeField] public float spawnInterval = 2f;
//    [SerializeField] public float obstacleLifetime = 5f;

//    [Header("Launch Settings")]
//    public float speed = 5f;

//    [Header("Target Settings")]
//    public bool useEndPoint = true;
//    [SerializeField] private Vector3 localEndPoint = Vector3.forward * 5f;

//    [Header("Auto Setup Options")]
//    [SerializeField] public bool autoAddRigidbody = true;
//    [SerializeField] public bool autoAddCollider = true;
//    [SerializeField] public PhysicsMaterial physicMaterial;
//    [SerializeField] public LayerMask obstacleLayer = 1;

//    [Header("Spawn Control")]
//    public bool spawnOnStart = true;
//    public bool autoSpawn = true;

//    private GameObject processedPrefab;

//    // Turret spawn point is always this transform
//    public Vector3 SpawnPoint => transform.position;

//    public Vector3 WorldEndPoint
//    {
//        get { return transform.position + transform.TransformDirection(localEndPoint); }
//        set { localEndPoint = transform.InverseTransformDirection(value - transform.position); }
//    }

//    public Vector3 LaunchDirection
//    {
//        get
//        {
//            if (useEndPoint)
//                return (WorldEndPoint - SpawnPoint).normalized;
//            return transform.TransformDirection(Vector3.forward);
//        }
//    }

//    public GameObject BasePrefab
//    {
//        get { return basePrefab; }
//        set
//        {
//            basePrefab = value;
//            processedPrefab = null; // Reset processed prefab when changed
//        }
//    }

//    public string SelectedObstacleType
//    {
//        get { return selectedObstacleType; }
//        set
//        {
//            selectedObstacleType = value;
//            processedPrefab = null; // Reset processed prefab when type changed
//        }
//    }

//    private void Start()
//    {
//        if (spawnOnStart && basePrefab != null)
//            SpawnObstacle();

//        if (autoSpawn && basePrefab != null)
//            StartCoroutine(AutoSpawn());
//    }

//    [ContextMenu("Setup Obstacle Prefab")]
//    public GameObject SetupObstaclePrefab()
//    {
//        if (basePrefab == null)
//        {
//            Debug.LogWarning("No base prefab assigned!");
//            return null;
//        }

//        // Create a temporary copy to setup
//        GameObject prefabCopy = Instantiate(basePrefab);
//        prefabCopy.name = basePrefab.name + "_" + selectedObstacleType;

//        // Add the appropriate ObstacleBase script based on type
//        ObstacleBase obstacleComponent = AddObstacleComponent(prefabCopy, selectedObstacleType);
//        if (obstacleComponent == null)
//        {
//            Debug.LogError($"Failed to add obstacle component for type: {selectedObstacleType}");
//            DestroyImmediate(prefabCopy);
//            return null;
//        }

//        if (autoAddRigidbody)
//        {
//            Rigidbody rb = prefabCopy.GetComponent<Rigidbody>();
//            if (rb == null)
//            {
//                rb = prefabCopy.AddComponent<Rigidbody>();
//                rb.useGravity = selectedObstacleType.Contains("Gravity");
//                Debug.Log($"✅ Added Rigidbody to {prefabCopy.name}");
//            }
//        }

//        if (autoAddCollider)
//        {
//            Collider col = prefabCopy.GetComponent<Collider>();
//            if (col == null)
//            {
//                MeshRenderer meshRenderer = prefabCopy.GetComponent<MeshRenderer>();
//                MeshFilter meshFilter = prefabCopy.GetComponent<MeshFilter>();

//                if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
//                {
//                    MeshCollider meshCol = prefabCopy.AddComponent<MeshCollider>();
//                    meshCol.sharedMesh = meshFilter.sharedMesh;
//                    meshCol.convex = true;
//                    if (physicMaterial != null)
//                        meshCol.material = physicMaterial;
//                    Debug.Log($"✅ Added MeshCollider to {prefabCopy.name}");
//                }
//                else
//                {
//                    BoxCollider boxCol = prefabCopy.AddComponent<BoxCollider>();
//                    if (physicMaterial != null)
//                        boxCol.material = physicMaterial;
//                    Debug.Log($"✅ Added BoxCollider to {prefabCopy.name}");
//                }
//            }
//            else if (physicMaterial != null)
//                col.material = physicMaterial;
//        }

//        // Set up trigger for player detection
//        Collider triggerCol = prefabCopy.GetComponent<Collider>();
//        if (triggerCol != null)
//        {
//            triggerCol.isTrigger = true;
//        }

//        prefabCopy.layer = Mathf.RoundToInt(Mathf.Log(obstacleLayer.value, 2));
//        processedPrefab = prefabCopy;

//        Debug.Log($"✅ Setup complete for {selectedObstacleType} obstacle");
//        return prefabCopy;
//    }

//    private ObstacleBase AddObstacleComponent(GameObject target, string typeName)
//    {
//        // Remove existing ObstacleBase components first
//        ObstacleBase[] existingComponents = target.GetComponents<ObstacleBase>();
//        for (int i = 0; i < existingComponents.Length; i++)
//        {
//            DestroyImmediate(existingComponents[i]);
//        }

//        // Find and add the component by type name
//        Type obstacleType = Type.GetType(typeName);
//        if (obstacleType == null)
//        {
//            Debug.LogError($"Could not find obstacle type: {typeName}");
//            return null;
//        }

//        if (!typeof(ObstacleBase).IsAssignableFrom(obstacleType))
//        {
//            Debug.LogError($"Type {typeName} does not inherit from ObstacleBase");
//            return null;
//        }

//        return target.AddComponent(obstacleType) as ObstacleBase;
//    }

//    [ContextMenu("Spawn Obstacle")]
//    public void SpawnObstacle()
//    {
//        if (basePrefab == null)
//        {
//            Debug.LogWarning("No base prefab assigned!");
//            return;
//        }

//        if (processedPrefab == null)
//        {
//            processedPrefab = SetupObstaclePrefab();
//            if (processedPrefab == null)
//            {
//                Debug.LogError("Failed to setup obstacle prefab!");
//                return;
//            }
//        }

//        // Instantiate the processed prefab
//        GameObject spawnedObj = Instantiate(processedPrefab, SpawnPoint, Quaternion.identity);
//        ObstacleBase spawnedObstacle = spawnedObj.GetComponent<ObstacleBase>();

//        if (spawnedObstacle != null)
//        {
//            spawnedObstacle.Init(LaunchDirection, speed);
//            Debug.Log($"✅ Spawned {selectedObstacleType} obstacle from turret");

//            // Auto-destroy after lifetime
//            StartCoroutine(DestroyAfterLifetime(spawnedObstacle));
//        }
//        else
//        {
//            Debug.LogWarning($"Failed to spawn {selectedObstacleType} obstacle.");
//            Destroy(spawnedObj);
//        }
//    }

//    private IEnumerator AutoSpawn()
//    {
//        while (autoSpawn && basePrefab != null)
//        {
//            SpawnObstacle();
//            yield return new WaitForSeconds(spawnInterval);
//        }
//    }

//    private IEnumerator DestroyAfterLifetime(ObstacleBase obstacle)
//    {
//        yield return new WaitForSeconds(obstacleLifetime);
//        if (obstacle != null && obstacle.gameObject != null)
//        {
//            Destroy(obstacle.gameObject);
//        }
//    }

//    [ContextMenu("Reset Spawner")]
//    public void ResetSpawner()
//    {
//        if (processedPrefab != null)
//        {
//            DestroyImmediate(processedPrefab);
//            processedPrefab = null;
//        }
//        StopAllCoroutines();
//        if (autoSpawn && basePrefab != null)
//            StartCoroutine(AutoSpawn());
//    }

//    public void StartSpawning()
//    {
//        if (!autoSpawn)
//        {
//            autoSpawn = true;
//            StartCoroutine(AutoSpawn());
//        }
//    }

//    public void StopSpawning()
//    {
//        autoSpawn = false;
//        StopAllCoroutines();
//    }

//    private void OnDrawGizmos()
//    {
//        Vector3 startPos = SpawnPoint;
//        Vector3 endPos = WorldEndPoint;
//        Vector3 direction = LaunchDirection;

//        // Draw launch direction
//        Gizmos.color = Color.red;

//        if (useEndPoint)
//        {
//            Gizmos.DrawLine(startPos, endPos);
//            DrawArrowhead(endPos, direction, 0.5f);
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawSphere(endPos, 0.1f);
//        }
//        else
//        {
//            Gizmos.DrawLine(startPos, startPos + direction * 2f);
//            DrawArrowhead(startPos + direction * 2f, direction, 0.3f);
//        }

//        // Draw turret position
//        Gizmos.color = Color.blue;
//        Gizmos.DrawSphere(startPos, 0.15f);

//        // Draw turret base indicator
//        Gizmos.color = Color.cyan;
//        Gizmos.DrawWireCube(startPos, Vector3.one * 0.3f);

//#if UNITY_EDITOR
//        if (basePrefab != null)
//            UnityEditor.Handles.Label(startPos + Vector3.up * 0.5f, $"Turret: {selectedObstacleType}");

//        if (useEndPoint)
//        {
//            float distance = Vector3.Distance(startPos, endPos);
//            UnityEditor.Handles.Label(Vector3.Lerp(startPos, endPos, 0.5f), $"{distance:F1}m");
//        }
//#endif
//    }

//    private void DrawArrowhead(Vector3 position, Vector3 direction, float size)
//    {
//        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * size;
//        Vector3 up = Vector3.Cross(right, direction).normalized * size;

//        Gizmos.DrawLine(position, position - direction * size + right * 0.5f);
//        Gizmos.DrawLine(position, position - direction * size - right * 0.5f);
//        Gizmos.DrawLine(position, position - direction * size + up * 0.5f);
//        Gizmos.DrawLine(position, position - direction * size - up * 0.5f);
//    }

//    private void OnDrawGizmosSelected()
//    {
//        if (useEndPoint)
//        {
//            Gizmos.color = Color.cyan;
//            Gizmos.DrawWireSphere(WorldEndPoint, 0.2f);
//        }

//        // Show spawn area
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireSphere(SpawnPoint, 0.3f);
//    }

//    private void OnDestroy()
//    {
//        // Clean up processed prefab when spawner is destroyed
//        if (processedPrefab != null)
//        {
//            DestroyImmediate(processedPrefab);
//        }
//    }
//}