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

    // Cached rigidbody for movement and collision
    private Rigidbody rb;

    private void Reset()
    {
        // Ensure components are present when the script is first added in the editor
        EnsureComponents();
    }

    private void Awake()
    {
        // Ensure required components exist at runtime (in case prefab was misconfigured)
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        // Make sure we have a Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Set recommended defaults so projectiles move correctly when velocity is applied
        rb.useGravity = false;                       // projectiles usually shouldn't fall immediately
        rb.isKinematic = false;                      // must be dynamic for velocity to move it
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

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
        // Only damage the player if we actually hit the player object (by tag)
        if (!hitObject.CompareTag("Player"))
        {
            return;
        }

        // Try to get the Player component from the hit object first
        Player player = hitObject.GetComponent<Player>();
        if (player == null)
        {
            // Fallback to singleton instance if needed
            player = Player.Instance;
        }

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
