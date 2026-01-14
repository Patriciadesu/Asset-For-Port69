using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Sprint Module")]
[Tooltip("Temporarily increases chase speed while in the Chase state.")]
public class SprintModule : EnemyModule
{
    [Header("Sprint Settings")]
    [Range(1f, 5f)] public float SprintMultiplier = 1.5f;
    [Tooltip("How long a single sprint burst lasts (seconds).")]
    public float SprintDuration = 2f;
    [Tooltip("Cooldown between sprint bursts (seconds).")]
    public float SprintCooldown = 4f;

    [Header("Chance Settings")]
    [SerializeField] private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.3f,
        Interval = new Vector2(2f, 4f),
        InitialDelay = new Vector2(0.5f, 1.5f)
    };

    private float originalChaseSpeed;
    private bool isSprinting;
    private float sprintEndTime;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        if (killer != null)
            originalChaseSpeed = killer.ChaseSpeed;
        chaseTrigger?.Prime();
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (newState != EnemyState.Chase)
        {
            StopSprint();
        }
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (!IsActive || killer == null) return;
        if (oldState == EnemyState.Chase)
        {
            StopSprint();
        }
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null)
            return;

        if (currentState != EnemyState.Chase)
        {
            StopSprint();
            return;
        }

        if (isSprinting)
        {
            if (Time.time >= sprintEndTime)
            {
                StopSprint();
                chaseTrigger?.BlockFor(SprintCooldown);
            }
            return;
        }

        chaseTrigger?.PrimeIfNeeded();
        if (chaseTrigger != null && chaseTrigger.TryConsumeTrigger())
        {
            StartSprint();
            chaseTrigger.BlockFor(SprintDuration + SprintCooldown);
        }
    }

    private void StartSprint()
    {
        if (killer == null)
            return;

        originalChaseSpeed = killer.ChaseSpeed;
        killer.ChaseSpeed = originalChaseSpeed * SprintMultiplier;
        isSprinting = true;
        sprintEndTime = Time.time + SprintDuration;
    }

    private void StopSprint()
    {
        if (!isSprinting || killer == null)
            return;

        killer.ChaseSpeed = originalChaseSpeed;
        isSprinting = false;
    }
}
