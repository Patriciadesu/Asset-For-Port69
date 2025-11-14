using UnityEngine;

/// <summary>
/// Projectile attack module for the Killer AI.
/// Fires projectiles at the player when within firing range.
/// Inherits attack range and damage from KillerAI base parameters.
/// </summary>
public class ProjectileModule : EnemyModule
{
    [Header("Projectile Settings")]
    [Tooltip("The projectile prefab to instantiate")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Transform where projectiles spawn from (e.g., hand or weapon point)")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Initial velocity of the projectile")]
    [SerializeField] private float projectileSpeed = 15f;

    [Tooltip("Range at which this module will attempt to fire (uses killer.AttackRange if 0)")]
    [SerializeField] private float firingRange = 0f;

    [Tooltip("Animation state name to trigger for projectile attack")]
    [SerializeField] private string shootAnimationName = "Demon|Shoot1";

    [Tooltip("Minimum time between projectile shots")]
    [SerializeField] private float shootCooldown = 1.5f;

    private Animator animator;
    private float lastShotTime = -10f;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        animator = killer.GetAnimator();

        // If firePoint not assigned, use killer's transform
        if (firePoint == null)
        {
            firePoint = killer.transform;
        }

        Debug.Log("[ProjectileModule] Initialized with firing range: " + (firingRange > 0 ? firingRange.ToString() : killer.AttackRange.ToString()));
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        // Only attempt to shoot during Chase state (not Attack state, as that's reserved for melee)
        if (currentState != EnemyState.Chase)
            return;

        Player target = GetTargetPlayer();
        if (target == null)
            return;

        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(killer.transform.position, target.transform.position);

        // Determine effective firing range
        float effectiveFiringRange = firingRange > 0 ? firingRange : killer.AttackRange;

        // Check if target is in range and cooldown has passed
        if (distanceToTarget <= effectiveFiringRange && Time.time >= lastShotTime + shootCooldown)
        {
            FireProjectile(target.transform.position);
            lastShotTime = Time.time;
        }
    }

    /// <summary>
    /// Fires a projectile towards the target position
    /// </summary>
    private void FireProjectile(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectileModule] Projectile prefab not assigned!");
            return;
        }

        // Trigger shoot animation with proper verification
        if (animator != null)
        {
            if (animator.parameters != null && animator.parameters.Length > 0)
            {
                bool hasShootTrigger = System.Array.Exists(animator.parameters, p => p.name == "Shoot" && p.type == AnimatorControllerParameterType.Trigger);
                if (hasShootTrigger)
                {
                    animator.SetTrigger("Shoot");
                    Debug.Log($"[ProjectileModule] Playing animation: {shootAnimationName}");
                }
                else
                {
                    Debug.LogWarning($"[ProjectileModule] Animator does not have 'Shoot' trigger parameter!");
                }
            }
            else
            {
                Debug.LogWarning("[ProjectileModule] Animator has no parameters!");
            }
        }
        else
        {
            Debug.LogWarning("[ProjectileModule] Animator reference is null!");
        }

        // Instantiate projectile at fire point
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - firePoint.position).normalized;

        // Apply velocity to projectile (if it has a Rigidbody)
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = directionToTarget * projectileSpeed;
        }

        // Rotate projectile to face direction of travel
        projectile.transform.rotation = Quaternion.LookRotation(directionToTarget);

        // Get animation duration for stun timing
        float animDuration = GetAnimationDuration(shootAnimationName);
        Debug.Log($"[ProjectileModule] Fired projectile at target. Distance: {Vector3.Distance(killer.transform.position, targetPosition)}, Animation duration: {animDuration}s");
    }

    /// <summary>
    /// Gets the duration of a specific animation clip
    /// </summary>
    private float GetAnimationDuration(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f; // fallback

        string shortName = animationName.Contains("|") ? animationName.Split('|')[1] : animationName;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == shortName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning($"[ProjectileModule] Animation clip '{shortName}' not found in animator!");
        return 0.5f; // fallback
    }

    /// <summary>
    /// Provides custom stun duration based on shoot animation length
    /// </summary>
    public override float? GetStunDuration(float baseAnimationDuration)
    {
        // Projectiles have minimal stun - just the animation time
        return baseAnimationDuration * 0.8f;
    }

    /// <summary>
    /// Helper to get the effective firing range
    /// </summary>
    public float GetEffectiveFiringRange()
    {
        return firingRange > 0 ? firingRange : killer.AttackRange;
    }
}
