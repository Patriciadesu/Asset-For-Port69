using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Killer AI/Modules/Dash Module")]
[Tooltip("Provides a short burst of movement towards a direction or target.")]
public class DashModule : EnemyModule
{
    [Header("Dash Settings")]
    public float DashDistance = 5f;
    public float DashDuration = 0.3f; // How long the dash takes
    public float DashCooldown = 3f;
    [Tooltip("Allow the module to automatically dash during chase using random rolls.")]
    public bool EnableChaseDash = true;

    [Header("Chance Settings")]
    [SerializeField]
    private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.3f,
        Interval = new Vector2(2f, 4f),
        InitialDelay = new Vector2(0.5f, 1.2f)
    };

    private float cooldownTimer = 0f;
    private NavMeshAgent agent;

    // Dash movement variables
    private bool isDashing = false;
    private Vector3 dashStartPos;
    private Vector3 dashTargetPos;
    private float dashTimer = 0f;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);

        // Prefer the NavMeshAgent managed by KillerAI, fall back to local component.
        agent = killer != null ? killer.GetAgent() : GetComponent<NavMeshAgent>();

        chaseTrigger?.Prime();
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Handle ongoing dash movement
        if (isDashing)
        {
            UpdateDashMovement();
        }

        if (!IsActive || !EnableChaseDash || killer == null)
            return;

        if (currentState != EnemyState.Chase)
            return;

        if (cooldownTimer > 0f || isDashing)
            return;

        chaseTrigger?.PrimeIfNeeded();
        if (chaseTrigger != null && chaseTrigger.TryConsumeTrigger())
        {
            if (TryDashTowardsTarget())
            {
                chaseTrigger.BlockFor(DashCooldown);
            }
            else
            {
                chaseTrigger.BlockFor(1f);
            }
        }
    }

    private void UpdateDashMovement()
    {
        dashTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(dashTimer / DashDuration);

        // Use a smooth curve for the dash (ease-out)
        float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

        Vector3 currentPos = Vector3.Lerp(dashStartPos, dashTargetPos, smoothProgress);

        // Move using NavMeshAgent if available, otherwise move transform directly
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(currentPos);
        }
        else
        {
            transform.position = currentPos;
        }

        // End dash when complete
        if (progress >= 1f)
        {
            isDashing = false;
            dashTimer = 0f;
        }
    }

    public bool TryDashTowardsTarget()
    {
        if (!IsActive || killer == null || killer.Target == null)
            return false;
        if (cooldownTimer > 0f || isDashing)
            return false;

        Vector3 dir = (killer.Target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            return false;
        dir.Normalize();

        Vector3 targetPosition = transform.position + dir * DashDistance;

        // Try to grab an agent if Initialize() didn't for some reason
        if (agent == null)
        {
            agent = killer != null ? killer.GetAgent() : GetComponent<NavMeshAgent>();
        }

        // Snap the dash end point to the navmesh if possible
        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, DashDistance, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
            }
        }

        // Start the dash
        dashStartPos = transform.position;
        dashTargetPos = targetPosition;
        isDashing = true;
        dashTimer = 0f;

        cooldownTimer = DashCooldown;
        return true;
    }
}