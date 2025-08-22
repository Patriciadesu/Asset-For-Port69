using UnityEngine;
using System.Collections;

[System.Serializable]
public class WaypointData
{
    public Vector3 localPosition;
    public float waitTime = 0f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public WaypointData(Vector3 pos)
    {
        localPosition = pos;
        waitTime = 0f;
        speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    }
}
public class ZigZagEffect : ObjectEffect
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private bool usePhysics = false;
    [SerializeField] private bool warpToStart = true;
    [SerializeField] private bool loopPath = false;
    [SerializeField] private bool debugMode = false;
    [Space(10)]
    [SerializeField]
    private WaypointData[] waypoints = new WaypointData[]
    {
        new WaypointData(new Vector3(0, 0, 5)),
        new WaypointData(new Vector3(5, 0, 10)),
        new WaypointData(new Vector3(-5, 0, 15)),
        new WaypointData(new Vector3(0, 0, 20))
    };
    [Space(10)]
    [Header("Gizmo Settings")]
    [SerializeField] public bool showGizmos = true;
    [SerializeField] public Color waypointColor = Color.cyan;
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private float waypointSize = 0.5f;
    private Coroutine activeCoroutine;
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            if (activeCoroutine != null)
            {
                player.StopCoroutine(activeCoroutine);
            }
            activeCoroutine = player.StartCoroutine(MoveAlongWaypoints(player));
            if (debugMode)
            {
                Debug.Log($"{gameObject.name} started waypoint movement for {player.gameObject.name}");
            }
        }
    }
    private IEnumerator MoveAlongWaypoints(Player player)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No waypoints defined!");
            yield break;
        }
        Vector3 startPosition = player.transform.position;
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (warpToStart && waypoints.Length > 0)
        {
            Vector3 firstWorldPos = transform.TransformPoint(waypoints[0].localPosition);
            if (usePhysics && playerRb != null)
            {
                playerRb.MovePosition(firstWorldPos);
            }
            else
            {
                player.transform.position = firstWorldPos;
            }
            if (debugMode)
            {
                Debug.Log($"Warped {player.gameObject.name} to first waypoint: {firstWorldPos}");
            }
        }
        int startIndex = warpToStart ? 1 : 0;
        do
        {
            for (int i = startIndex; i < waypoints.Length; i++)
            {
                yield return StartCoroutine(MoveToWaypoint(player, playerRb, waypoints[i], i));
                if (waypoints[i].waitTime > 0)
                {
                    yield return new WaitForSeconds(waypoints[i].waitTime);
                }
            }
            startIndex = 0;
        } while (loopPath);
        activeCoroutine = null;
        if (debugMode)
        {
            Debug.Log($"Waypoint movement completed for {player.gameObject.name}");
        }
    }
    private IEnumerator MoveToWaypoint(Player player, Rigidbody playerRb, WaypointData waypoint, int waypointIndex)
    {
        Vector3 startPos = player.transform.position;
        Vector3 targetPos = transform.TransformPoint(waypoint.localPosition);
        float distance = Vector3.Distance(startPos, targetPos);
        float journeyTime = distance / moveSpeed;
        float elapsedTime = 0f;
        if (debugMode)
        {
            Debug.Log($"Moving to waypoint {waypointIndex}: {targetPos} (Distance: {distance:F1}, Time: {journeyTime:F1}s)");
        }
        while (elapsedTime < journeyTime)
        {
            float t = elapsedTime / journeyTime;
            float speedMultiplier = waypoint.speedCurve.Evaluate(t);
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            if (usePhysics && playerRb != null)
            {
                playerRb.MovePosition(currentPos);
            }
            else
            {
                player.transform.position = currentPos;
            }
            elapsedTime += Time.deltaTime * speedMultiplier;
            yield return null;
        }
        if (usePhysics && playerRb != null)
        {
            playerRb.MovePosition(targetPos);
        }
        else
        {
            player.transform.position = targetPos;
        }
    }
    public void AddWaypoint()
    {
        Vector3 newPos = Vector3.zero;
        if (waypoints.Length > 0)
        {
            newPos = waypoints[waypoints.Length - 1].localPosition + Vector3.forward * 5f;
        }
        System.Array.Resize(ref waypoints, waypoints.Length + 1);
        waypoints[waypoints.Length - 1] = new WaypointData(newPos);
    }
    public void RemoveWaypoint(int index)
    {
        if (waypoints.Length > 1 && index >= 0 && index < waypoints.Length)
        {
            var newWaypoints = new WaypointData[waypoints.Length - 1];
            for (int i = 0, j = 0; i < waypoints.Length; i++)
            {
                if (i != index)
                {
                    newWaypoints[j] = waypoints[i];
                    j++;
                }
            }
            waypoints = newWaypoints;
        }
    }
    public void ClearWaypoints()
    {
        waypoints = new WaypointData[] { new WaypointData(Vector3.zero) };
    }
    public WaypointData[] GetWaypoints()
    {
        return waypoints;
    }
    public void SetWaypoints(WaypointData[] newWaypoints)
    {
        waypoints = newWaypoints;
    }
    private void OnDrawGizmos()
    {
        if (!showGizmos || waypoints == null || waypoints.Length == 0) return;
        DrawWaypointGizmos(false);
    }
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || waypoints == null || waypoints.Length == 0) return;
        DrawWaypointGizmos(true);
    }
    private void DrawWaypointGizmos(bool selected)
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = selected ? waypointColor : waypointColor * 0.7f;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(waypoints[i].localPosition);
            Gizmos.DrawSphere(worldPos, waypointSize);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPos + Vector3.up * (waypointSize + 0.5f), i.ToString());
#endif
        }
        Gizmos.color = selected ? pathColor : pathColor * 0.7f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 from = transform.TransformPoint(waypoints[i].localPosition);
            Vector3 to = transform.TransformPoint(waypoints[i + 1].localPosition);
            Gizmos.DrawLine(from, to);
            Vector3 direction = (to - from).normalized;
            Vector3 arrowHead1 = to - direction * 0.5f - Vector3.Cross(direction, Vector3.up) * 0.3f;
            Vector3 arrowHead2 = to - direction * 0.5f + Vector3.Cross(direction, Vector3.up) * 0.3f;
            Gizmos.DrawLine(to, arrowHead1);
            Gizmos.DrawLine(to, arrowHead2);
        }
        if (loopPath && waypoints.Length > 2)
        {
            Vector3 lastPos = transform.TransformPoint(waypoints[waypoints.Length - 1].localPosition);
            Vector3 firstPos = transform.TransformPoint(waypoints[0].localPosition);
            Gizmos.color = pathColor * 0.5f;
            Gizmos.DrawLine(lastPos, firstPos);
        }
        Gizmos.color = oldColor;
    }
}