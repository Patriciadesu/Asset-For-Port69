using UnityEngine;

/// <summary>
/// Simple projectile component for Killer AI that damages the player on contact.
/// Attach this to projectile prefabs used by ProjectileModule.
/// Alternative: You can also use the existing EnemyProjectile class.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KillerAIProjectile : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage dealt to the player on hit")]
    public float Damage = 10f;

    [Header("Lifetime Settings")]
    [Tooltip("Time in seconds before the projectile is destroyed (0 = never)")]
    public float Lifetime = 5f;

    [Tooltip("Destroy projectile on impact")]
    public bool DestroyOnHit = true;

    private void Start()
    {
        // Auto-destroy after lifetime expires
        if (Lifetime > 0)
        {
            Destroy(gameObject, Lifetime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Check if we hit the player
        Player player = Player.Instance;
        if (player != null && player.Stat != null)
        {
            // Deal damage to the player
            player.Stat.TakeDamage(Damage);
            Debug.Log($"[KillerAIProjectile] Hit player for {Damage} damage!");
        }

        // Destroy projectile if configured
        if (DestroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
