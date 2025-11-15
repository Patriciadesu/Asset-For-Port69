using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Rage Module")]
[Tooltip("Applies temporary stat multipliers while rage is enabled.")]
public class RageModule : EnemyModule
{
    [Header("Rage Settings")]
    public float SpeedMultiplier = 1.5f;
    public float AttackRangeMultiplier = 1.2f;

    public enum RageTriggerCondition
    {
        Manual,
        OnChaseEnter,
        OnAttackEnter
    }

    [Header("Trigger Settings")]
    [Tooltip("Determines when rage should automatically activate.")]
    public RageTriggerCondition TriggerCondition = RageTriggerCondition.Manual;
    [Tooltip("Optional delay before rage activates once the trigger condition is met.")]
    public float TriggerDelay = 0f;
    [Tooltip("If true, rage will only trigger once per life.")]
    public bool TriggerOnlyOnce = true;
    [Tooltip("Automatically disable rage when leaving this state. Set to EnemyState.Idle to disable when combat ends, etc.")]
    public bool AutoDisableOnStateExit = true;
    public EnemyState AutoDisableState = EnemyState.Idle;

    private bool isRaging = false;
    private float originalPatrol;
    private float originalChase;
    private float originalAttackRange;
    private bool hasTriggered;
    private Coroutine pendingTriggerRoutine;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        if (killer != null)
        {
            originalPatrol = killer.PatrolSpeed;
            originalChase = killer.ChaseSpeed;
            originalAttackRange = killer.AttackRange;
        }
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive)
            return;

        if (ShouldTriggerForState(newState))
        {
            TryScheduleRage();
        }
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (!IsActive)
            return;

        if (AutoDisableOnStateExit && isRaging && oldState == AutoDisableState)
        {
            DisableRage();
        }
    }

    public void EnableRage()
    {
        if (!IsActive || killer == null || isRaging) return;
        isRaging = true;
        originalPatrol = killer.PatrolSpeed;
        originalChase = killer.ChaseSpeed;
        originalAttackRange = killer.AttackRange;
        killer.PatrolSpeed *= SpeedMultiplier;
        killer.ChaseSpeed *= SpeedMultiplier;
        killer.AttackRange *= AttackRangeMultiplier;
    }

    public void DisableRage()
    {
        if (killer == null || !isRaging) return;
        isRaging = false;
        killer.PatrolSpeed = originalPatrol;
        killer.ChaseSpeed = originalChase;
        killer.AttackRange = originalAttackRange;
    }

    private bool ShouldTriggerForState(EnemyState state)
    {
        switch (TriggerCondition)
        {
            case RageTriggerCondition.OnChaseEnter:
                return state == EnemyState.Chase;
            case RageTriggerCondition.OnAttackEnter:
                return state == EnemyState.Attack;
            default:
                return false;
        }
    }

    private void TryScheduleRage()
    {
        if (TriggerOnlyOnce && hasTriggered)
            return;

        if (pendingTriggerRoutine != null)
        {
            StopCoroutine(pendingTriggerRoutine);
            pendingTriggerRoutine = null;
        }

        if (TriggerDelay <= 0f)
        {
            EnableRage();
            hasTriggered = true;
        }
        else
        {
            pendingTriggerRoutine = StartCoroutine(ActivateAfterDelay());
        }
    }

    private System.Collections.IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(TriggerDelay);
        EnableRage();
        hasTriggered = true;
        pendingTriggerRoutine = null;
    }
}
