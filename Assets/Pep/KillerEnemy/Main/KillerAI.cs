using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Main AI controller using a state machine pattern with modular capabilities.
/// Manages core AI behavior and delegates to attached EnemyModule components.
/// Uses NavMeshAgent for all movement.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class KillerAI : MonoBehaviour
{
    #region State Machine
    [Header("State Machine")]
    [Tooltip("Current state of the AI - visible for debugging")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    /// <summary>
    /// Public read-only access to the current AI state
    /// </summary>
    public EnemyState CurrentState
    {
        get => currentState;
        private set => currentState = value;
    }
    #endregion

    #region Targeting
    [Header("Targeting")]
    [Tooltip("The player to target and damage")]
    public Player TargetPlayer;

    [Tooltip("Distance at which the AI starts chasing the target")]
    public float ChaseRange = 10f;

    [Tooltip("Distance at which the AI can attack")]
    public float AttackRange = 2f;

    /// <summary>
    /// Helper property to get the target's transform for backwards compatibility
    /// </summary>
    public Transform Target => TargetPlayer != null ? TargetPlayer.transform : null;
    #endregion

    #region Movement
    [Header("Movement")]
    [Tooltip("Movement speed during patrol")]
    public float PatrolSpeed = 3f;

    [Tooltip("Movement speed during chase")]
    public float ChaseSpeed = 6f;
    #endregion

    #region NavMesh and Imperfect Chase
    [Header("NavMesh/Chase (Imperfect)")]
    [Tooltip("Reference to NavMeshAgent used for pathfinding")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("How often to pick a new destination while chasing")]
    [SerializeField] private float repathInterval = 1.2f;

    [Tooltip("Random extra delay added to each repath to simulate hesitation")]
    [SerializeField] private Vector2 repathJitter = new Vector2(0.5f, 1.0f);

    [Tooltip("Inaccuracy radius added around the player when setting destinations")]
    [SerializeField] private float inaccuracyRadius = 3.5f;

    [Tooltip("Radius for NavMesh.SamplePosition when placing noisy targets")]
    [SerializeField] private float sampleRadius = 4.0f;

    [Tooltip("Distance to stop from the player while chasing")]
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Imperfect Pathfinding")]
    [Tooltip("Chance (0-1) to pick a suboptimal path when encountering obstacles")]
    [SerializeField] private float confusionChance = 0.35f;

    [Tooltip("When confused, how far off-course to go")]
    [SerializeField] private float confusionRadius = 4.0f;

    [Tooltip("Chance to briefly pause when encountering obstacles")]
    [SerializeField] private float hesitationChance = 0.25f;

    [Tooltip("How long to pause when hesitating")]
    [SerializeField] private Vector2 hesitationDuration = new Vector2(0.3f, 0.8f);

    [Tooltip("Should the AI sometimes take wrong turns around corners?")]
    [SerializeField] private bool allowWrongTurns = true;

    [Tooltip("Chance to take a wrong turn at corners")]
    [SerializeField] private float wrongTurnChance = 0.2f;

    private float nextRepathAt = 0f;
    private bool isHesitating = false;
    private float hesitationEndTime = 0f;
    private Vector3 lastTargetPosition;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    #endregion

    #region Chase Difficulty
    public enum ChaseDifficulty { Easy, Medium, Hard, Custom }

    [Header("Chase Difficulty Mode")]
    [SerializeField] private ChaseDifficulty difficulty = ChaseDifficulty.Medium;

    // Custom values - shown only when difficulty == Custom
    [SerializeField] private float customChaseSpeed = 5f;
    [SerializeField] private float customRotationSpeed = 3f;
    [SerializeField] private float customDetectionRange = 15f;
    [SerializeField] private float customStopDistance = 2f;
    [SerializeField] private float customInaccuracyRadius = 2.5f;
    [SerializeField] private float customRepathInterval = 1.0f;
    [SerializeField] private Vector2 customRepathJitter = new Vector2(0.5f, 0.8f);
    [SerializeField] private float customAgentAngularSpeed = 200f;
    [SerializeField] private float customAgentAcceleration = 8f;
    [SerializeField] private ObstacleAvoidanceType customAvoidance = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    [Range(0, 99)] [SerializeField] private int customAvoidancePriority = 50;
    #endregion

    #region Attack
    [Header("Attack")]
    [Tooltip("Duration of the attack animation/state")]
    public float AttackDuration = 1f;

    [Tooltip("Cooldown between attacks")]
    public float AttackCooldown = 2f;

    private float attackTimer = 0f;
    #endregion

    #region Modules
    [Header("Modules")]
    [Tooltip("All discovered modules on this GameObject")]
    [SerializeField] private List<EnemyModule> modules = new List<EnemyModule>();

    /// <summary>
    /// Public read-only access to the module list for debugging
    /// </summary>
    public List<EnemyModule> Modules => modules;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Get required component
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // Configure agent for less-than-perfect chasing
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(40, 70);
            agent.autoBraking = true; // Changed to true for more realistic stopping
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.stoppingDistance = stopDistance;
            agent.isStopped = true;
            agent.speed = PatrolSpeed;

            // Make pathfinding less optimal
            agent.areaMask = NavMesh.AllAreas; // Can go anywhere, making choices harder

            lastTargetPosition = Vector3.zero;
            lastPosition = transform.position;
            ScheduleNextRepath();
        }
        else
        {
            Debug.LogError("[KillerAI] NavMeshAgent component is missing!");
        }

        // Apply difficulty settings
        ApplyDifficultySettings();

        // Discover and initialize all modules
        DiscoverModules();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep module list in sync in the editor for accurate inspector display
        DiscoverModules();
        ApplyDifficultySettings();
    }
#endif

    private void Start()
    {
        // Initialize all modules
        InitializeModules();

        // Start in Idle state
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        // Update all active modules
        UpdateModules();

        // Execute current state logic
        UpdateCurrentState();
    }
    #endregion

    #region Module Management
    /// <summary>
    /// Discovers all EnemyModule components attached to this GameObject
    /// </summary>
    private void DiscoverModules()
    {
        modules.Clear();

        // Find all modules on this GameObject
        EnemyModule[] foundModules = GetComponents<EnemyModule>();

        if (foundModules != null && foundModules.Length > 0)
        {
            modules.AddRange(foundModules);
            if (Application.isPlaying)
                Debug.Log($"[KillerAI] Discovered {modules.Count} module(s)");
        }
        else
        {
            if (Application.isPlaying)
                Debug.Log("[KillerAI] No modules found - AI will run with default behavior");
        }
    }

    /// <summary>
    /// Initializes all discovered modules
    /// </summary>
    private void InitializeModules()
    {
        foreach (var module in modules)
        {
            if (module != null)
            {
                module.Initialize(this);
                Debug.Log($"[KillerAI] Initialized module: {module.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Updates all active modules every frame
    /// </summary>
    private void UpdateModules()
    {
        foreach (var module in modules)
        {
            if (module != null && module.IsActive)
            {
                module.OnStateUpdate(CurrentState);
            }
        }
    }
    #endregion

    /// <summary>
    /// Editor helper to refresh modules list when components are added/removed in edit mode.
    /// </summary>
    public void RefreshModulesInEditor()
    {
        DiscoverModules();
    }

    #region State Machine
    /// <summary>
    /// Transitions the AI to a new state, calling appropriate module callbacks
    /// </summary>
    /// <param name="newState">The state to transition to</param>
    public void ChangeState(EnemyState newState)
    {
        // Don't change if already in this state
        if (CurrentState == newState)
            return;

        EnemyState oldState = CurrentState;

        // Notify modules of state exit
        foreach (var module in modules)
        {
            if (module != null && module.IsActive)
            {
                module.OnStateExit(oldState);
            }
        }

        // Change state
        CurrentState = newState;

        // Debug output
        Debug.Log($"[KillerAI] State changed: {oldState} → {newState}");

        // Notify modules of state enter
        foreach (var module in modules)
        {
            if (module != null && module.IsActive)
            {
                module.OnStateEnter(newState);
            }
        }
    }

    /// <summary>
    /// Main state machine update logic
    /// </summary>
    private void UpdateCurrentState()
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
                IdleUpdate();
                break;
            case EnemyState.Patrol:
                PatrolUpdate();
                break;
            case EnemyState.Chase:
                ChaseUpdate();
                break;
            case EnemyState.Attack:
                AttackUpdate();
                break;
        }
    }
    #endregion

    #region State Logic
    private void IdleUpdate()
    {
        // Stop agent while idle
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // If target is within chase range, begin chasing
        if (Target != null)
        {
            float d = Vector3.Distance(transform.position, Target.position);
            if (d <= ChaseRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }
        }
        // Otherwise, modules (e.g., PatrolModule) can decide next actions
    }

    private void PatrolUpdate()
    {
        // Enable agent for patrol movement (PatrolModule manages it via NavMesh)
        // Don't stop the agent here - let PatrolModule control it

        // Allow core AI to transition to Chase when player enters ChaseRange
        if (Target != null)
        {
            float d = Vector3.Distance(transform.position, Target.position);
            if (d <= ChaseRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }
        }
        // Movement during patrol is managed by PatrolModule using NavMeshAgent
    }

    private void ChaseUpdate()
    {
        // Validate target
        if (Target == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, Target.position);

        // If target left chase range, stop chasing
        if (distanceToTarget > ChaseRange)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // Check if within attack range
        if (distanceToTarget <= AttackRange && attackTimer <= 0f)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Handle hesitation state (pausing when confused)
        if (isHesitating)
        {
            if (Time.time >= hesitationEndTime)
            {
                isHesitating = false;
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
                return; // Don't do anything while hesitating
            }
        }

        // Start/drive NavMeshAgent-based imperfect chase
        if (agent != null)
        {
            // Ensure agent is active and moving
            agent.isStopped = false;
            agent.speed = ChaseSpeed;
            agent.stoppingDistance = stopDistance > 0f ? stopDistance : AttackRange;

            // Check if we're stuck (not moving much)
            float movementThisFrame = Vector3.Distance(transform.position, lastPosition);
            if (movementThisFrame < 0.01f && agent.velocity.sqrMagnitude < 0.1f)
            {
                stuckTimer += Time.deltaTime;
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;

            // Detect if target changed direction significantly
            bool targetMovedAlot = Vector3.Distance(Target.position, lastTargetPosition) > 3.0f;
            lastTargetPosition = Target.position;

            // Set destination immediately if we don't have a path, it's time to repath, or stuck
            if (!agent.hasPath || Time.time >= nextRepathAt || stuckTimer > 1.0f || targetMovedAlot)
            {
                // Sometimes get confused and pick wrong destination
                if (Random.value < confusionChance)
                {
                    SetConfusedDestination();

                    // Maybe hesitate when confused
                    if (Random.value < hesitationChance)
                    {
                        TriggerHesitation();
                    }
                }
                else if (allowWrongTurns && Random.value < wrongTurnChance && IsNearCorner())
                {
                    // Take a wrong turn at corners
                    SetWrongTurnDestination();
                }
                else
                {
                    // Normal imperfect pathfinding
                    SetNoisyDestination();
                }

                ScheduleNextRepath();
                stuckTimer = 0f; // Reset stuck timer after repathing
            }

            // If stuck even after repathing, try a small wander to unstick
            if (!agent.pathPending && agent.hasPath &&
                agent.remainingDistance > agent.stoppingDistance &&
                agent.velocity.sqrMagnitude < 0.05f &&
                stuckTimer > 2.0f)
            {
                SetNearbyWander();
                stuckTimer = 0f;
            }
        }

        // Update attack cooldown
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    private void AttackUpdate()
    {
        // Stop while attacking
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Validate target
        if (Target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // Face the target during attack
        Vector3 directionToTarget = (Target.position - transform.position).normalized;
        directionToTarget.y = 0f; // Keep rotation on horizontal plane
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Execute attack (placeholder - add animation triggers here)
        attackTimer += Time.deltaTime;

        if (attackTimer >= AttackDuration)
        {
            // Attack finished, set cooldown and return to chase
            attackTimer = -AttackCooldown; // Negative timer for cooldown
            ChangeState(EnemyState.Chase);

            // Here you would trigger damage to the target
            Debug.Log("[KillerAI] Attack executed!");
        }
    }

    // Helper methods for imperfect chase using NavMesh
    private void SetNoisyDestination()
    {
        if (Target == null || agent == null) return;

        // Add random inaccuracy around the target position
        Vector3 desired = Target.position + Random.insideUnitSphere * inaccuracyRadius;
        desired.y = Target.position.y;

        // Try to find a valid position on the NavMesh
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Fallback to direct target position if sampling fails
            agent.SetDestination(Target.position);
        }
    }

    private void SetConfusedDestination()
    {
        if (Target == null || agent == null) return;

        // Pick a destination that's notably off from the target
        Vector3 directionToTarget = (Target.position - transform.position).normalized;

        // Add perpendicular offset to simulate "going the wrong way around an obstacle"
        Vector3 perpendicular = Vector3.Cross(directionToTarget, Vector3.up);
        float side = Random.value > 0.5f ? 1f : -1f;

        Vector3 confused = Target.position + (perpendicular * side * confusionRadius);
        confused += Random.insideUnitSphere * 2.0f; // Additional randomness
        confused.y = Target.position.y;

        if (NavMesh.SamplePosition(confused, out NavMeshHit hit, sampleRadius * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("[KillerAI] Got confused! Taking suboptimal path.");
        }
        else
        {
            SetNoisyDestination(); // Fallback to normal noisy destination
        }
    }

    private void SetWrongTurnDestination()
    {
        if (Target == null || agent == null) return;

        // Calculate direction to target
        Vector3 directionToTarget = (Target.position - transform.position).normalized;

        // Pick the opposite direction (wrong way around corner)
        Vector3 wrongDirection = Quaternion.Euler(0, Random.Range(60f, 120f), 0) * directionToTarget;
        Vector3 wrongDestination = transform.position + wrongDirection * 5f;
        wrongDestination.y = transform.position.y;

        if (NavMesh.SamplePosition(wrongDestination, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("[KillerAI] Took wrong turn!");
        }
        else
        {
            SetNoisyDestination(); // Fallback
        }
    }

    private void SetNearbyWander()
    {
        if (agent == null) return;

        // Pick a random point near the AI to help it get unstuck
        Vector3 aroundSelf = transform.position + Random.insideUnitSphere * 2.0f;
        aroundSelf.y = transform.position.y;

        if (NavMesh.SamplePosition(aroundSelf, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("[KillerAI] Wandering to get unstuck.");
        }
    }

    private void TriggerHesitation()
    {
        isHesitating = true;
        hesitationEndTime = Time.time + Random.Range(hesitationDuration.x, hesitationDuration.y);
        Debug.Log("[KillerAI] Hesitating...");
    }

    private bool IsNearCorner()
    {
        // Simple check: if we have a path and the next corner requires significant turn
        if (agent == null || !agent.hasPath || agent.path.corners.Length < 2)
            return false;

        Vector3 currentDir = transform.forward;
        Vector3 nextCorner = agent.path.corners[1] - transform.position;
        nextCorner.y = 0;
        nextCorner.Normalize();

        float angle = Vector3.Angle(currentDir, nextCorner);
        return angle > 45f; // Consider it a corner if turn is more than 45 degrees
    }

    private void ScheduleNextRepath()
    {
        float jitter = Random.Range(repathJitter.x, repathJitter.y);
        nextRepathAt = Time.time + repathInterval + jitter;
    }
    #endregion

    #region Public API for Modules
    /// <summary>
    /// Finds and assigns the Player as the target
    /// </summary>
    public void FindPlayer()
    {
        Player player = Player.Instance;
        
        // Validate that the player instance is actually valid and in the scene
        if (player != null && player.gameObject != null && player.gameObject.scene.IsValid())
        {
            TargetPlayer = player;
            Debug.Log($"[KillerAI] Found and assigned Player: {player.name}");
        }
        else
        {
            // Player.Instance exists but is invalid - try to find player manually
            Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            if (allPlayers != null && allPlayers.Length > 0)
            {
                TargetPlayer = allPlayers[0];
                Debug.Log($"[KillerAI] Found and assigned Player via search: {allPlayers[0].name}");
            }
            else
            {
                Debug.LogWarning("[KillerAI] No Player found in scene! Please add a GameObject with the Player script.");
            }
        }
    }

    /// <summary>
    /// Damages the target player by the specified amount
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    /// <returns>True if damage was applied successfully</returns>
    public bool DamagePlayer(float damageAmount)
    {
        if (TargetPlayer != null && TargetPlayer.Stat != null)
        {
            float newHealth = TargetPlayer.Stat.currenthealth - damageAmount;
            TargetPlayer.Stat.currenthealth = Mathf.Max(0, newHealth);
            Debug.Log($"[KillerAI] Dealt {damageAmount} damage to player. Player health: {TargetPlayer.Stat.currenthealth}/{TargetPlayer.Stat.maxhealth}");
            return true;
        }
        else
        {
            Debug.LogWarning("[KillerAI] Cannot damage player - TargetPlayer or Stat is null!");
            return false;
        }
    }

    /// <summary>
    /// Gets the NavMeshAgent component (for modules that need direct access)
    /// </summary>
    public NavMeshAgent GetAgent()
    {
        return agent;
    }

    /// <summary>
    /// Manually set a destination for the NavMeshAgent (useful for patrol modules)
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// Stop the agent's movement
    /// </summary>
    public void StopMovement()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    /// <summary>
    /// Resume agent movement with specified speed
    /// </summary>
    public void ResumeMovement(float speed)
    {
        if (agent != null)
        {
            agent.speed = speed;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Switch to a difficulty preset or Custom mode
    /// </summary>
    public void SetDifficulty(ChaseDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficultySettings();
    }

    /// <summary>
    /// Set custom chase values at runtime
    /// </summary>
    public void SetCustomChaseValues(
        float chaseSpeed,
        float rotationSpeed,
        float detectionRange,
        float stopDist,
        float inaccuracy,
        float repathInterval,
        Vector2 jitter,
        float agentAngularSpeed,
        float agentAcceleration,
        ObstacleAvoidanceType avoidance,
        int avoidancePriority)
    {
        customChaseSpeed = chaseSpeed;
        customRotationSpeed = rotationSpeed;
        customDetectionRange = detectionRange;
        customStopDistance = stopDist;
        customInaccuracyRadius = inaccuracy;
        customRepathInterval = repathInterval;
        customRepathJitter = jitter;
        customAgentAngularSpeed = agentAngularSpeed;
        customAgentAcceleration = agentAcceleration;
        customAvoidance = avoidance;
        customAvoidancePriority = Mathf.Clamp(avoidancePriority, 0, 99);
        SetDifficulty(ChaseDifficulty.Custom);
    }

    /// <summary>
    /// Apply difficulty preset or custom settings to chase parameters
    /// </summary>
    private void ApplyDifficultySettings()
    {
        switch (difficulty)
        {
            case ChaseDifficulty.Easy:
                ChaseSpeed = 4f;
                ChaseRange = 15f;
                stopDistance = 2.5f;
                inaccuracyRadius = 4.5f;
                repathInterval = 1.5f;
                repathJitter = new Vector2(0.7f, 1.2f);
                confusionChance = 0.4f;
                hesitationChance = 0.3f;
                if (agent != null)
                {
                    agent.angularSpeed = 120f;
                    agent.acceleration = 3f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                    agent.avoidancePriority = 70;
                }
                break;

            case ChaseDifficulty.Medium:
                ChaseSpeed = 6f;
                ChaseRange = 12f;
                stopDistance = 1.8f;
                inaccuracyRadius = 2.5f;
                repathInterval = 0.9f;
                repathJitter = new Vector2(0.4f, 0.8f);
                confusionChance = 0.25f;
                hesitationChance = 0.15f;
                if (agent != null)
                {
                    agent.angularSpeed = 200f;
                    agent.acceleration = 6f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
                    agent.avoidancePriority = 50;
                }
                break;

            case ChaseDifficulty.Hard:
                ChaseSpeed = 8f;
                ChaseRange = 20f;
                stopDistance = 1.2f;
                inaccuracyRadius = 0.8f;
                repathInterval = 0.4f;
                repathJitter = new Vector2(0.1f, 0.3f);
                confusionChance = 0.1f;
                hesitationChance = 0.05f;
                if (agent != null)
                {
                    agent.angularSpeed = 360f;
                    agent.acceleration = 12f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.avoidancePriority = 30;
                }
                break;

            case ChaseDifficulty.Custom:
                ChaseSpeed = customChaseSpeed;
                ChaseRange = customDetectionRange;
                stopDistance = customStopDistance;
                inaccuracyRadius = customInaccuracyRadius;
                repathInterval = customRepathInterval;
                repathJitter = customRepathJitter;
                if (agent != null)
                {
                    agent.angularSpeed = customAgentAngularSpeed;
                    agent.acceleration = customAgentAcceleration;
                    agent.obstacleAvoidanceType = customAvoidance;
                    agent.avoidancePriority = customAvoidancePriority;
                }
                break;
        }

        // Apply settings to agent
        if (agent != null)
        {
            agent.stoppingDistance = stopDistance;
        }
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        // Draw chase and attack ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // Draw current path if agent exists and has a path
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] path = agent.path.corners;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw more detailed debug info when selected
        if (agent != null && Application.isPlaying)
        {
            // Draw velocity direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, agent.velocity);

            // Draw destination
            if (agent.hasPath)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(agent.destination, 0.5f);
            }
        }
    }
    #endregion
}