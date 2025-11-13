using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Walk Module")]
[Tooltip("Defines default walking speed for patrol.")]
public class WalkModule : EnemyModule
{
    [Header("Walk Settings")]
    public float DefaultPatrolSpeed = 3f;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        if (killer != null && DefaultPatrolSpeed > 0f)
        {
            killer.PatrolSpeed = DefaultPatrolSpeed;
        }
    }
}
