using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Slash Module")]
[Tooltip("Performs a short-range melee slash on attack enter.")]
public class SlashModule : EnemyModule
{
    [Header("Slash Settings")]
    [Tooltip("Maximum range at which the slash can hit")]
    public float DamageRange = 2f;

    [Tooltip("Amount of damage dealt by the slash")]
    public float SlashDamage = 15f;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (newState == EnemyState.Attack && killer.Target != null)
        {
            float d = Vector3.Distance(transform.position, killer.Target.position);
            if (d <= DamageRange)
            {
                Player player = Player.Instance;
                if (player != null && player.Stat != null)
                {
                    player.Stat.TakeDamage(SlashDamage);
                    Debug.Log($"[SlashModule] Slash hit target for {SlashDamage} damage!");
                }
                else
                {
                    Debug.LogWarning("[SlashModule] Slash hit but couldn't apply damage!");
                }
            }
            else
            {
                Debug.Log("[SlashModule] Slash missed");
            }
        }
    }
}
