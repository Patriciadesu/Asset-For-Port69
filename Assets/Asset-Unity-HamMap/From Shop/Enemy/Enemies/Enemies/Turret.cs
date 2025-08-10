using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public enum ShootingType
    {
        ShootToPlayer,
        PredictPlayerMove
    };
    public enum DetectionBehavior
    {
        ConeRotate,
        ConeStatic,
        Area
    };

    [Header("Turret Settings")]
    [SerializeField] ShootingType shootingType;
    [SerializeField] DetectionBehavior detectionBehavior;
    [SerializeField] bool showDetectionRange = true;
    public float detectionRange = 10f;
    public float detectionTime = 1.5f;
    public float fireRate = 1f;
    public float rotateSpeed = 45f;
    public float maxDetectionAngle = 30f;
    public float rotationOffset = 0;
    public Vector3 front => Quaternion.Euler(0, rotationOffset, 0) * transform.forward;
    private Vector3 initiateFront;

    [Header("Bullet Settings")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] private float bulletLifeTime = 2f;
    [SerializeField] private float bulletSpeed = 5f;

    [HideInInspector] public Player player;
    [HideInInspector] public float detectionAngle;
    private State currentState;
    private LineRenderer lineRenderer;  //  LineRenderer for visualizing detection area
    private struct PositionSample
    {
        public Vector3 position;
        public float time;

        public PositionSample(Vector3 pos, float t)
        {
            position = pos;
            time = t;
        }
    }
    private List<PositionSample> positionHistory = new List<PositionSample>();
    private float sampleInterval = 0.5f;                // Time between samples (adjustable)
    private float maxHistoryTime = 2f;                  // How long to keep samples
    private float nextSampleTime;
    private void Start()
    {
        gameObject.tag = "Enemy";
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        detectionAngle = maxDetectionAngle;
        SetState(new IdleState(this));
        initiateFront = front;

        //  Initialize LineRenderer for real-time visualization
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = 20; // Number of points in the arc
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.enabled = showDetectionRange;
    }

    private void Update()
    {
        currentState.Update();
        TrackPlayerMovement();
        DrawDetectionArea(); //  Update detection visualization in real-time
    }

    public void DetectionBehaviorHandler()
    {
        switch (detectionBehavior)
        {
            case DetectionBehavior.ConeRotate:
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
                break;
            default:
                break;
        }
    }

    public void SetState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public bool PlayerInRange()
    {
        return Vector3.Distance(transform.position, player.transform.TransformPoint(player.capsuleCollider.center)) <= detectionRange;
    }

    public bool IsPlayerInSight()
    {
        switch (detectionBehavior)
        {
            case DetectionBehavior.Area:
                bool isInRange = PlayerInRange();
                return isInRange;
            case DetectionBehavior.ConeStatic:
                Vector3 staticDirectionToPlayer = (player.transform.TransformPoint(player.capsuleCollider.center) - transform.position).normalized;
                float staticAngle = Vector3.Angle(initiateFront, staticDirectionToPlayer);
                return staticAngle <= detectionAngle;
            case DetectionBehavior.ConeRotate:
                Vector3 directionToPlayer = (player.transform.TransformPoint(player.capsuleCollider.center) - transform.position).normalized;
                float angle = Vector3.Angle(front, directionToPlayer);
                return angle <= detectionAngle;
            default:
                return false;
        }
    }

    public void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        switch (shootingType)
        {
            case ShootingType.ShootToPlayer:
                bullet.GetComponent<Rigidbody>().AddForce((player.transform.TransformPoint(player.capsuleCollider.center) - firePoint.position).normalized * bulletSpeed, ForceMode.Impulse);
                break;
            case ShootingType.PredictPlayerMove:
                Vector3 targetPosition = PredictPlayerPosition();
                bullet.GetComponent<Rigidbody>().AddForce((targetPosition - firePoint.position).normalized * bulletSpeed, ForceMode.Impulse);
                break;
        }
        Destroy(bullet, bulletLifeTime);
    }
    private void TrackPlayerMovement()
    {
        if (player == null || firePoint == null) return;

        float currentTime = Time.time;

        // Sample position at intervals
        if (currentTime >= nextSampleTime)
        {
            Vector3 currentPosition = player.transform.TransformPoint(player.capsuleCollider.center); // Using base position, adjust if needed
            positionHistory.Add(new PositionSample(currentPosition, currentTime));
            nextSampleTime = currentTime + sampleInterval;

            // Clean up old samples
            positionHistory.RemoveAll(sample => currentTime - sample.time > maxHistoryTime);
        }
    }

    public Vector3 PredictPlayerPosition(float predictionTime = 2f)
    {
        if (positionHistory.Count < 2)
        {
            Debug.LogWarning("Not enough position samples for prediction");
            return player != null ? player.transform.TransformPoint(player.capsuleCollider.center) : Vector3.zero;
        }

        // Calculate velocity from recent position changes
        Vector3 velocity = CalculateVelocityFromHistory();

        // Current position
        Vector3 currentPosition = player.transform.TransformPoint(player.capsuleCollider.center);

        // Calculate travel time based on distance
        float distanceToTarget = Vector3.Distance(firePoint.position, currentPosition);
        float travelTime = distanceToTarget / bulletSpeed;
        travelTime = Mathf.Min(travelTime, predictionTime);

        // Predict position
        Vector3 predictedPosition = currentPosition + velocity * travelTime;

        // Debug visualization
        Debug.DrawLine(firePoint.position, predictedPosition, Color.green, 1f);

        return predictedPosition;
    }

    private Vector3 CalculateVelocityFromHistory()
    {
        if (positionHistory.Count < 2) return Vector3.zero;

        // Use the two most recent samples
        PositionSample newest = positionHistory[positionHistory.Count - 1];
        PositionSample previous = positionHistory[positionHistory.Count - 2];

        float timeDelta = newest.time - previous.time;
        if (timeDelta <= 0f) return Vector3.zero;

        Vector3 positionDelta = newest.position - previous.position;
        return positionDelta / timeDelta;
    }

    public void ClearHistory()
    {
        positionHistory.Clear();
    }
    private void DrawDetectionArea()
    {
        if (detectionBehavior == DetectionBehavior.Area)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        float angleStep = detectionAngle * 2 / (lineRenderer.positionCount - 1);
        float startAngle = -detectionAngle;

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);

            Vector3 point;
            if (detectionBehavior == DetectionBehavior.ConeRotate)
            {
                point = transform.position + (rotation * front) * detectionRange;
            }
            else // ConeStatic
            {
                point = transform.position + (rotation * initiateFront) * detectionRange;
            }

            lineRenderer.SetPosition(i, point);
        }

        // Ensure the first point is at the turret position
        lineRenderer.SetPosition(0, transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, front);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 leftBoundary;
        Vector3 rightBoundary;

        switch (detectionBehavior)
        {
            case DetectionBehavior.ConeRotate:
                leftBoundary = Quaternion.Euler(0, -maxDetectionAngle, 0) * front;
                rightBoundary = Quaternion.Euler(0, maxDetectionAngle, 0) * front;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
                Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
                break;

            case DetectionBehavior.ConeStatic:
                if (initiateFront == Vector3.zero)
                {
                    leftBoundary = Quaternion.Euler(0, -maxDetectionAngle, 0) * front;
                    rightBoundary = Quaternion.Euler(0, maxDetectionAngle, 0) * front;
                }
                else
                {
                    leftBoundary = Quaternion.Euler(0, -maxDetectionAngle, 0) * initiateFront;
                    rightBoundary = Quaternion.Euler(0, maxDetectionAngle, 0) * initiateFront;
                }
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
                Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
                break;
        }
    }
}

//   STATE MACHINE
public abstract class State
{
    protected Turret turret;
    public State(Turret turret) { this.turret = turret; }
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

public class IdleState : State
{
    public IdleState(Turret turret) : base(turret) { }

    public override void Enter()
    {
        turret.detectionAngle = 30f;
    }

    public override void Update()
    {
        turret.DetectionBehaviorHandler();

        if (turret.PlayerInRange() && turret.IsPlayerInSight())
            turret.SetState(new DetectingState(turret));
    }

    public override void Exit() { }
}

public class DetectingState : State
{
    private float timer;
    public DetectingState(Turret turret) : base(turret) { }

    public override void Enter()
    {
        timer = turret.detectionTime;
    }

    public override void Update()
    {
        turret.detectionAngle = Mathf.Lerp(30f, 60f, 1 - (timer / turret.detectionTime));

        Vector3 direction = (turret.player.transform.TransformPoint(turret.player.capsuleCollider.center) - turret.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        turret.transform.rotation = Quaternion.Slerp(turret.transform.rotation, lookRotation, Time.deltaTime * 2);

        timer -= Time.deltaTime;
        if (timer <= 0)
            turret.SetState(new ShootingState(turret));
        else if (!turret.PlayerInRange() || !turret.IsPlayerInSight())
            turret.SetState(new IdleState(turret));
    }

    public override void Exit() { }
}

public class ShootingState : State
{
    private float fireCooldown;
    public ShootingState(Turret turret) : base(turret) { }

    public override void Enter()
    {
        fireCooldown = 0;
    }

    public override void Update()
    {
        if (!turret.PlayerInRange() || !turret.IsPlayerInSight())
        {
            turret.SetState(new IdleState(turret));
            return;
        }

        Vector3 direction = (turret.player.transform.TransformPoint(turret.player.capsuleCollider.center) - turret.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        turret.transform.rotation = Quaternion.Slerp(turret.transform.rotation, lookRotation, Time.deltaTime * 2);

        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0)
        {
            turret.Fire();
            fireCooldown = turret.fireRate;
        }
    }

    public override void Exit() { }
}
