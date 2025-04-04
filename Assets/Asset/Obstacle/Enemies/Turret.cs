using System;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public enum ShootingType
    {
        ToPlayer,
        Straight
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
    [SerializeField] bool canDestroy = false;
    public float detectionRange = 10f;
    public float detectionTime = 1.5f;
    public float fireRate = 1f;
    public float rotateSpeed = 45f;
    public float maxDetectionAngle = 30f;
    private Vector3 initiateFront;

    [Header("Bullet Settings")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] private float bulletLifeTime = 2f;
    [SerializeField] private float bulletSpeed = 5f;

    [HideInInspector] public Transform player;
    [HideInInspector] public float detectionAngle;
    private State currentState;
    private LineRenderer lineRenderer;  //  LineRenderer for visualizing detection area

    private void Start()
    {
        gameObject.tag = "Enemy";
        player = GameObject.FindGameObjectWithTag("Player").transform;
        detectionAngle = maxDetectionAngle;
        SetState(new IdleState(this));
        initiateFront = transform.forward;

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
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    public bool IsPlayerInSight()
    {
        switch (detectionBehavior)
        {
            case DetectionBehavior.Area:
                return PlayerInRange();
            case DetectionBehavior.ConeStatic:
                Vector3 staticDirectionToPlayer = (player.position - transform.position).normalized;
                float staticAngle = Vector3.Angle(initiateFront, staticDirectionToPlayer);
                return staticAngle <= detectionAngle;
            case DetectionBehavior.ConeRotate:
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
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
            case ShootingType.Straight:
                bullet.GetComponent<Rigidbody>().AddForce(transform.forward * bulletSpeed, ForceMode.Impulse);
                break;
            case ShootingType.ToPlayer:
                bullet.GetComponent<Rigidbody>().AddForce((player.transform.position - transform.position).normalized * bulletSpeed, ForceMode.Impulse);
                break;
        }
        bullet.GetComponent<Bullet>().owner = gameObject;
        Destroy(bullet, bulletLifeTime);
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
                point = transform.position + (rotation * transform.forward) * detectionRange;
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

    public void OnTriggerEnter(Collider other)
    {
        if (canDestroy)
        {
            if (other.TryGetComponent<Bullet>(out Bullet bullet))
            {
                if (bullet.owner.tag != "Enemy")
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 leftBoundary;
        Vector3 rightBoundary;

        switch (detectionBehavior)
        {
            case DetectionBehavior.ConeRotate:
                leftBoundary = Quaternion.Euler(0, -maxDetectionAngle, 0) * transform.forward;
                rightBoundary = Quaternion.Euler(0, maxDetectionAngle, 0) * transform.forward;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
                Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
                break;

            case DetectionBehavior.ConeStatic:
                if (initiateFront == Vector3.zero)
                {
                    leftBoundary = Quaternion.Euler(0, -maxDetectionAngle, 0) * transform.forward;
                    rightBoundary = Quaternion.Euler(0, maxDetectionAngle, 0) * transform.forward;
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

        Vector3 direction = (turret.player.position - turret.transform.position).normalized;
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

        Vector3 direction = (turret.player.position - turret.transform.position).normalized;
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
