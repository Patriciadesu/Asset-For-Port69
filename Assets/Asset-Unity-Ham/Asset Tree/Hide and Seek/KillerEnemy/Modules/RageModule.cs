using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Rage Module")]
[Tooltip("Applies temporary stat multipliers while rage is enabled.")]
public class RageModule : EnemyModule
{
    [Header("Rage Settings")]
    public float SpeedMultiplier = 1.5f;
    public float AttackRangeMultiplier = 1.2f;

    private bool isRaging = false;
    private float originalPatrol;
    private float originalChase;
    private float originalAttackRange;

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
}
