using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Light Sensitive Module")]
[Tooltip("Applies a speed penalty when marked as 'in light'. External systems can flip the InLight flag.")]
public class LightSensitiveModule : EnemyModule
{
    [Header("Light Sensitivity")]
    [Range(0.1f, 1f)] public float SpeedPenaltyMultiplier = 0.6f;
    public bool InLight = false;

    private float originalPatrol;
    private float originalChase;
    private bool applied;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        if (killer != null)
        {
            originalPatrol = killer.PatrolSpeed;
            originalChase = killer.ChaseSpeed;
        }
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null) return;

        if (InLight && !applied)
        {
            applied = true;
            originalPatrol = killer.PatrolSpeed;
            originalChase = killer.ChaseSpeed;
            killer.PatrolSpeed *= SpeedPenaltyMultiplier;
            killer.ChaseSpeed *= SpeedPenaltyMultiplier;
        }
        else if (!InLight && applied)
        {
            applied = false;
            killer.PatrolSpeed = originalPatrol;
            killer.ChaseSpeed = originalChase;
        }
    }
}
