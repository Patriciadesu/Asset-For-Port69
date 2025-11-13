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

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (SummonOnAttackEnter && newState == EnemyState.Attack)
        {
            Summon();
        }
    }

    public void Summon()
    {
        if (MinionPrefab == null) return;
        for (int i = 0; i < Mathf.Max(0, Count); i++)
        {
            float angle = (360f / Mathf.Max(1, Count)) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Radius;
            Object.Instantiate(MinionPrefab, transform.position + offset, Quaternion.identity);
        }
    }
}
