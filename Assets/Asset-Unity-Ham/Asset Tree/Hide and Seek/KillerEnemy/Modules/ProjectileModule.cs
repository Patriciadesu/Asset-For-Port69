using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Projectile Module")]
[Tooltip("Fires a projectile prefab on attack enter. For damage, add KillerAIProjectile or EnemyProjectile component to the prefab.")]
public class ProjectileModule : EnemyModule
{
    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab to spawn (should have KillerAIProjectile or EnemyProjectile component for damage)")]
    public GameObject ProjectilePrefab;

    [Tooltip("Spawn point for the projectile (uses enemy transform if null)")]
    public Transform FirePoint;

    [Tooltip("Initial velocity of the projectile")]
    public float MuzzleVelocity = 10f;

    [Tooltip("Fire projectile when entering attack state")]
    public bool FireOnAttackEnter = true;

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (FireOnAttackEnter && newState == EnemyState.Attack)
        {
            Fire();
        }
    }

    public void Fire()
    {
        if (!IsActive || ProjectilePrefab == null)
            return;

        Transform spawnXf = FirePoint != null ? FirePoint : transform;
        var go = Object.Instantiate(ProjectilePrefab, spawnXf.position, spawnXf.rotation);

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = spawnXf.forward * MuzzleVelocity;
        }
    }
}
