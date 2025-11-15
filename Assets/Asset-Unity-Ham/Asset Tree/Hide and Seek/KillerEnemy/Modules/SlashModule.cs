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

    [Header("Override Settings")]
    [Tooltip("If true, the slash fully replaces the KillerAI base attack logic.")]
    public bool OverrideDefaultAttack = true;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null) return;
        if (!OverrideDefaultAttack && newState == EnemyState.Attack)
        {
            ExecuteSlash();
        }
    }

    public override bool TryHandleAttackOverride()
    {
        if (!IsActive || killer == null || !OverrideDefaultAttack)
            return false;

        return ExecuteSlash();
    }

    private bool ExecuteSlash()
    {
        if (killer.Target == null)
            return false;

        float distance = Vector3.Distance(transform.position, killer.Target.position);
        bool inRange = distance <= DamageRange;

        if (inRange)
        {
            if (DamagePlayer(SlashDamage))
            {
                Debug.Log($"[SlashModule] Slash override dealt {SlashDamage} damage.");
            }
            else
            {
                Debug.LogWarning("[SlashModule] Slash override failed to apply damage!");
            }
        }
        else
        {
            Debug.Log("[SlashModule] Slash override triggered but target was out of range.");
        }

        return true;
    }
}
