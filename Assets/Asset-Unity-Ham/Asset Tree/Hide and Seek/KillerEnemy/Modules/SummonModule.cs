using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Summon Module")]
[Tooltip("Spawns helper units (minions) around the AI, e.g., on attack enter.")]
public class SummonModule : EnemyModule
{
    [Header("Summon Settings")]
    public GameObject MinionPrefab;
    public int Count = 1;
    public float Radius = 3f;
    public bool SummonOnAttackEnter = false;
    [Tooltip("How long summoned minions live before auto-despawning (seconds). Set <=0 to keep forever.")]
    public float MinionLifetime = 3f;

    [Header("Attack Override")]
    [Tooltip("If enabled, the module can replace the normal attack with a summon attempt.")]
    public bool AllowAttackOverride = true;
    [Range(0f, 1f)]
    public float OverrideChance = 0.35f;
    [Tooltip("Seconds before another override can happen.")]
    public float OverrideCooldown = 6f;
    [Tooltip("Only override if the target is within this distance.")]
    public float OverrideRange = 6f;
    public bool AlwaysOverrideWhenAvailable = false;

    private float nextOverrideTime = 0f;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (SummonOnAttackEnter && newState == EnemyState.Attack)
        {
            Summon();
        }
    }

    public override bool TryHandleAttackOverride()
    {
        if (!IsActive || !AllowAttackOverride || killer == null)
            return false;

        if (Time.time < nextOverrideTime)
            return false;

        bool shouldOverride = AlwaysOverrideWhenAvailable || Random.value <= OverrideChance;
        if (!shouldOverride)
            return false;

        if (killer.Target != null && OverrideRange > 0f)
        {
            float distance = Vector3.Distance(transform.position, killer.Target.position);
            if (distance > OverrideRange)
            {
                return false;
            }
        }

        Summon();
        nextOverrideTime = Time.time + Mathf.Max(0.5f, OverrideCooldown);
        Debug.Log("[SummonModule] Override triggered - spawned minions instead of normal attack.");
        return true;
    }

    public void Summon()
    {
        if (MinionPrefab == null) return;
        for (int i = 0; i < Mathf.Max(0, Count); i++)
        {
            float angle = (360f / Mathf.Max(1, Count)) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Radius;
            var clone = Object.Instantiate(MinionPrefab, transform.position + offset, Quaternion.identity);
            if (MinionLifetime > 0f && clone != null)
            {
                Object.Destroy(clone, MinionLifetime);
            }
        }
    }
}
