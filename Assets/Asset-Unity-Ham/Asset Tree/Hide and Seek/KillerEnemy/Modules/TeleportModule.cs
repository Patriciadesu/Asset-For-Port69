using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Teleport Module")]
[Tooltip("Provides simple teleport utilities and optional behavior on attack.")]
public class TeleportModule : EnemyModule
{
    [Header("Teleport Settings")]
    public float TeleportBehindDistance = 2.5f;
    public bool TeleportBehindTargetOnAttack = false;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (TeleportBehindTargetOnAttack && newState == EnemyState.Attack && killer.Target != null)
        {
            Vector3 dir = (killer.Target.position - transform.position).normalized;
            dir.y = 0f;
            Vector3 pos = killer.Target.position - dir * TeleportBehindDistance;
            transform.position = pos;
        }
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
    }
}
