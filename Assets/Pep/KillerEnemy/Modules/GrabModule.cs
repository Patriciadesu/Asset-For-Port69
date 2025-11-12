using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Grab Module")]
[Tooltip("Attempts to grab the target at very close range.")]
public class GrabModule : EnemyModule
{
    [Header("Grab Settings")]
    [Tooltip("Maximum range at which the grab can succeed")]
    public float GrabRange = 1.2f;

    [Tooltip("Amount of damage dealt when grabbing the target")]
    public float GrabDamage = 25f;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (newState == EnemyState.Attack && killer.Target != null)
        {
            float d = Vector3.Distance(transform.position, killer.Target.position);
            if (d <= GrabRange)
            {
                Player player = Player.Instance;
                if (player != null && player.Stat != null)
                {
                    player.Stat.TakeDamage(GrabDamage);
                    Debug.Log($"[GrabModule] Target grabbed for {GrabDamage} damage!");
                    // Hook: constraint/animation can be added here
                }
                else
                {
                    Debug.LogWarning("[GrabModule] Grab succeeded but couldn't apply damage!");
                }
            }
        }
    }
}
