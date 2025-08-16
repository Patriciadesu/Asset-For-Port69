using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Timeline;

/// <summary>
/// Base class for all graph conditions. Inherit this and override StartTrackCondition / StopTrackCondition.
/// </summary>
[System.Serializable]
public class Condition
{
    [HideInInspector] public UnityEvent onConditionMet = new UnityEvent();

    protected Boss boss;

    /// <summary> Bind the Boss that owns the UnityEvents this condition will listen to. </summary>
    public virtual void Bind(Boss bossRef)
    {
        boss = bossRef;
    }

    /// <summary> Called when the transition begins tracking this condition. Subscribe to boss events here. </summary>
    public virtual void StartTrackCondition(){}

    /// <summary> Called when the transition stops tracking this condition. Unsubscribe here. </summary>
    public virtual void StopTrackCondition(){}

    /// <summary> Helper to fire the condition. </summary>
    protected void Raise()
    {
        if (onConditionMet != null) onConditionMet.Invoke();
    }
}

public class PlayerInSightCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInSight.AddListener(OnFired);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInSight.RemoveListener(OnFired);
    }

    private void OnFired() => Raise();
}

public class PlayerOutOfSightCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfSight.AddListener(OnFired);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfSight.RemoveListener(OnFired);
    }

    private void OnFired() => Raise();
}

public class PlayerInAttackRangeCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInAttackRange.AddListener(OnFired);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInAttackRange.RemoveListener(OnFired);
    }

    private void OnFired() => Raise();
}

public class HealthBelowCondition : Condition
{
    [Tooltip("Trigger condition when health <= this value.")]
    public float threshold = 0.3f;

    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onHealthChanged.AddListener(OnHealth);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onHealthChanged.RemoveListener(OnHealth);
    }

    private void OnHealth(float health)
    {
        if (health <= threshold)
            Raise();
    }
}

public class StateTimeAtLeastCondition : Condition
{
    [Tooltip("Trigger when state time >= this value (seconds).")]
    public float timeThreshold = 1f;

    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onStateTimeChanged.AddListener(OnTime);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onStateTimeChanged.RemoveListener(OnTime);
    }

    private void OnTime(float t)
    {
        if (t >= timeThreshold)
            Raise();
    }
}

public class OnStateChangedCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onStateChanged.AddListener(OnFired);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onStateChanged.RemoveListener(OnFired);
    }

    private void OnFired() => Raise();
}
