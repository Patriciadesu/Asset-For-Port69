using System.Collections;
using UnityEngine;
using NaughtyAttributes; // ← add this
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField]private Transform endPoint;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float obstacleLifetime = 5f;

    [Header("Obstacle Prefab")]
    [ShowAssetPreview(96, 96)] 
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Debug")]
    private bool showGizmos = true;
    private Color gizmoColor = Color.red;

    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    public Transform EndPoint
    {
        get => endPoint;
        set => endPoint = value;
    }

    public float SpawnInterval
    {
        get => spawnInterval;
        set => spawnInterval = Mathf.Max(0.1f, value);
    }

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0.1f, value);
    }

    public float ObstacleLifetime
    {
        get => obstacleLifetime;
        set => obstacleLifetime = Mathf.Max(0.1f, value);
    }

    private void Start()
    {
        StartSpawning();
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        if (obstaclePrefab == null || endPoint == null)
        {
            Debug.LogWarning($"ObstacleSpawner on {gameObject.name}: Missing obstacle prefab or endpoint!");
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefab == null || endPoint == null) return;

        GameObject obstacleObj = Instantiate(obstaclePrefab, transform.position, transform.rotation);
        ObstacleBase obstacle = obstacleObj.GetComponent<ObstacleBase>();

        if (obstacle == null)
        {
            Debug.LogWarning($"Obstacle prefab {obstaclePrefab.name} doesn't have ObstacleBase component!");
            Destroy(obstacleObj);
            return;
        }

        Vector3 direction = (endPoint.position - transform.position).normalized;

        Debug.Log($"Spawning obstacle from {transform.position} to {endPoint.position}");
        Debug.Log($"Direction: {direction}, Speed: {speed}");

        obstacle.Init(direction, speed);

        Rigidbody obstacleRb = obstacleObj.GetComponent<Rigidbody>();
        if (obstacleRb != null)
        {
            Debug.Log($"Obstacle velocity after Init: {obstacleRb.linearVelocity}");
        }

        StartCoroutine(DestroyObstacleAfterTime(obstacle, obstacleLifetime));
    }

    private IEnumerator DestroyObstacleAfterTime(ObstacleBase obstacle, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (obstacle != null)
        {
            obstacle.Deactivate();
            Destroy(obstacle.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || endPoint == null) return;

        Gizmos.color = gizmoColor;

        Gizmos.DrawLine(transform.position, endPoint.position);

        Vector3 direction = (endPoint.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, direction).normalized;

        float arrowSize = 0.5f;
        Vector3 arrowTip = endPoint.position;
        Vector3 arrowBase = arrowTip - direction * arrowSize;

        Gizmos.DrawLine(arrowBase, arrowTip);
        Gizmos.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f + up * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f + up * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f - up * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f - up * arrowSize * 0.5f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(endPoint.position, Vector3.one * 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (endPoint == null) return;

        Vector3 direction = (endPoint.position - transform.position).normalized;
        float previewDistance = Vector3.Distance(transform.position, endPoint.position) * 0.3f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * previewDistance);

        Gizmos.color = Color.cyan;
        Vector3 midPoint = Vector3.Lerp(transform.position, endPoint.position, 0.5f);
        Gizmos.DrawWireCube(midPoint, Vector3.one * 0.1f);
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.1f, spawnInterval);
        speed = Mathf.Max(0.1f, speed);
        obstacleLifetime = Mathf.Max(0.1f, obstacleLifetime);
    }
}