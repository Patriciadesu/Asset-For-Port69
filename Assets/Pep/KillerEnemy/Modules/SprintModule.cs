using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Sprint Module")]
[Tooltip("Temporarily increases chase speed while in the Chase state.")]
public class SprintModule : EnemyModule
{
    [Header("Sprint Settings")]
    [Range(1f, 5f)] public float SprintMultiplier = 1.5f;

    private float originalChaseSpeed;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        if (killer != null)
            originalChaseSpeed = killer.ChaseSpeed;
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (newState == EnemyState.Chase)
        {
            originalChaseSpeed = killer.ChaseSpeed;
            killer.ChaseSpeed = originalChaseSpeed * SprintMultiplier;
        }
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (!IsActive || killer == null) return;
        if (oldState == EnemyState.Chase)
        {
            killer.ChaseSpeed = originalChaseSpeed;
        }
    }
}
