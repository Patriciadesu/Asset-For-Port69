using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Killer AI/Modules/Teleport Module")]
[Tooltip("Provides simple teleport utilities and optional behavior on attack.")]
public class TeleportModule : EnemyModule
{
    [Header("Teleport Settings")]
    public float TeleportBehindDistance = 2.5f;
    public bool TeleportBehindTargetOnAttack = false;
    [Tooltip("Enable automatic chase teleports triggered by chance rolls.")]
    public bool EnableChaseTeleport = true;
    [Tooltip("Extra cooldown after a successful teleport (seconds).")]
    public float PostTeleportCooldown = 4f;

    [Header("NavMesh Validation")]
    [Tooltip("Radius to search for valid NavMesh position")]
    public float NavMeshSampleRadius = 5f;

    [Tooltip("If true, only teleport to valid NavMesh positions")]
    public bool ValidateNavMesh = true;

    [Tooltip("Show debug info for teleport attempts")]
    public bool DebugTeleport = true;

    [Header("Chase Trigger")]
    [SerializeField] private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.35f,
        Interval = new Vector2(3f, 6f),
        InitialDelay = new Vector2(0.5f, 1.5f)
    };

    private NavMeshAgent agent;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        agent = GetComponent<NavMeshAgent>();
        chaseTrigger?.Prime();
        Debug.Log($"[TeleportModule] Initialized. NavMesh validation: {ValidateNavMesh}");
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (TeleportBehindTargetOnAttack && newState == EnemyState.Attack && killer.Target != null)
        {
            Vector3 dir = (killer.Target.position - transform.position).normalized;
            dir.y = 0f;
            Vector3 targetPos = killer.Target.position - dir * TeleportBehindDistance;
            
            if (TeleportTo(targetPos))
            {
                Debug.Log($"[TeleportModule] Teleported behind target at {targetPos}");
            }
            else
            {
                Debug.LogWarning($"[TeleportModule] Failed to teleport behind target - no valid NavMesh position found!");
            }
        }
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null || killer.Target == null)
            return;

        if (!EnableChaseTeleport || currentState != EnemyState.Chase)
            return;

        chaseTrigger?.PrimeIfNeeded();
        if (chaseTrigger != null && chaseTrigger.TryConsumeTrigger())
        {
            bool success = TeleportBehindTarget();
            float cooldown = success ? Mathf.Max(0.5f, PostTeleportCooldown) : 1.5f;
            chaseTrigger.BlockFor(cooldown);
        }
    }

    private bool TeleportBehindTarget()
    {
        if (killer?.Target == null)
            return false;

        Vector3 dir = (killer.Target.position - transform.position).normalized;
        dir.y = 0f;
        Vector3 targetPos = killer.Target.position - dir * TeleportBehindDistance;
        bool result = TeleportTo(targetPos);

        if (result)
        {
            Debug.Log("[TeleportModule] Chase teleport succeeded.");
        }
        else
        {
            Debug.LogWarning("[TeleportModule] Chase teleport failed.");
        }

        return result;
    }

    /// <summary>
    /// Teleports to a position, validating NavMesh if enabled.
    /// Returns true if teleport succeeded, false if position invalid.
    /// </summary>
    public bool TeleportTo(Vector3 targetPosition)
    {
        if (!IsActive)
        {
            if (DebugTeleport) Debug.LogWarning("[TeleportModule] Teleport attempted but module is inactive!");
            return false;
        }

        // If NavMesh validation is disabled, teleport directly
        if (!ValidateNavMesh)
        {
            transform.position = targetPosition;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(targetPosition);
            }
            if (DebugTeleport) Debug.Log($"[TeleportModule] Teleported to {targetPosition} (no validation)");
            return true;
        }

        // Validate position is on NavMesh
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
        {
            Vector3 validPosition = hit.position;
            
            // Set position
            transform.position = validPosition;
            
            // If using NavMeshAgent, warp it to the new position
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(validPosition);
                if (DebugTeleport) Debug.Log($"[TeleportModule] Teleported to {validPosition} (on NavMesh, agent warped)");
            }
            else if (agent != null)
            {
                if (DebugTeleport) Debug.LogWarning("[TeleportModule] Teleported but agent not on NavMesh");
            }
            else
            {
                if (DebugTeleport) Debug.Log($"[TeleportModule] Teleported to {validPosition} (no agent, position only)");
            }
            
            return true;
        }
        else
        {
            if (DebugTeleport) Debug.LogWarning($"[TeleportModule] Teleport failed - position {targetPosition} not on NavMesh! Searched radius: {NavMeshSampleRadius}");
            return false;
        }
    }
}
