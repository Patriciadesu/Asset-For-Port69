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

    [Tooltip("Spawn the projectile near the end of the animation (0-1)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float spawnTimePercent = 1f;

    /// <summary>
    /// Public accessor for the fire point so helper scripts (e.g., EnemyHand) can assign it.
    /// </summary>
    public Transform FirePoint
    {
        get => firePoint;
        set => firePoint = value;
    }

    private Animator animator;
    private float lastShotTime = -10f;
    private bool isShooting = false;      // true while performing a projectile attack in Shooting state
    private Vector3 pendingTargetPosition;
    private Coroutine shootingCoroutine;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        animator = killer.GetAnimator();

        // If firePoint not assigned, try to use EnemyHand marker; otherwise fall back to killer transform
        if (firePoint == null && killer != null)
        {
            EnemyHand hand = killer.GetComponentInChildren<EnemyHand>();
            if (hand != null)
            {
                firePoint = hand.transform;
            }
            else
            {
                firePoint = killer.transform;
            }
        }

        Debug.Log("[ProjectileModule] Initialized with firing range: " + (firingRange > 0 ? firingRange.ToString() : killer.AttackRange.ToString()));
    }

    private void OnDisable()
    {
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
            shootingCoroutine = null;
        }

        isShooting = false;
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null)
            return;

        Player target = GetTargetPlayer();
        if (target == null)
            return;

        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(killer.transform.position, target.transform.position);

        // Determine effective firing range
        float effectiveFiringRange = firingRange > 0 ? firingRange : killer.AttackRange;

        if (currentState == EnemyState.Chase)
        {
            // Only start a new projectile attack if not already in the middle of one
            if (!isShooting &&
                distanceToTarget <= effectiveFiringRange &&
                Time.time >= lastShotTime + shootCooldown)
            {
                // Remember where we want to shoot and ask the main AI to switch to Shooting state
                pendingTargetPosition = target.transform.position;
                isShooting = true;

                killer.ChangeState(EnemyState.Shooting);
            }

            return;
        }

        // While in Shooting state, keep looking at the target (optional)
        if (currentState == EnemyState.Shooting && isShooting)
        {
            Vector3 dir = (target.transform.position - killer.transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                killer.transform.rotation = Quaternion.Slerp(killer.transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || killer == null)
            return;

        // When we enter Shooting state because of this module, start the shoot animation.
        if (newState == EnemyState.Shooting && isShooting)
        {
            BeginShoot(pendingTargetPosition);
        }
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (oldState == EnemyState.Shooting)
        {
            // If leaving Shooting early (e.g., interrupted), stop any pending shot.
            if (shootingCoroutine != null)
            {
                StopCoroutine(shootingCoroutine);
                shootingCoroutine = null;
            }

            isShooting = false;
        }
    }

    /// <summary>
    /// Starts the shoot process: plays animation; projectile is spawned when animation finishes.
    /// </summary>
    private void BeginShoot(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectileModule] Projectile prefab not assigned!");
            isShooting = false;

            if (killer != null && killer.CurrentState == EnemyState.Shooting)
            {
                killer.ChangeState(EnemyState.Chase);
            }
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning("[ProjectileModule] Animator reference is null, firing immediately without animation.");
            SpawnProjectile(targetPosition);
            lastShotTime = Time.time;
            isShooting = false;

            if (killer != null && killer.CurrentState == EnemyState.Shooting)
            {
                killer.ChangeState(EnemyState.Chase);
            }
            return;
        }

        // Stop movement while we perform the shooting animation
        killer.StopMovement();

        // Verify that we have a "Shoot" trigger parameter before using it
        bool hasShootTrigger = false;
        if (animator.parameters != null && animator.parameters.Length > 0)
        {
            hasShootTrigger = System.Array.Exists(
                animator.parameters,
                p => p.name == "Shoot" && p.type == AnimatorControllerParameterType.Trigger);
        }

        if (hasShootTrigger)
        {
            animator.SetTrigger("Shoot");
            Debug.Log($"[ProjectileModule] Playing shoot animation: {shootAnimationName}");
        }
        else
        {
            Debug.LogWarning("[ProjectileModule] Animator does not have 'Shoot' trigger parameter! Projectile will still be spawned.");
        }

        // Cache the position we were aiming at when we started the throw
        pendingTargetPosition = targetPosition;

        // Start coroutine that waits for the animation, then spawns the projectile
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
        }
        shootingCoroutine = StartCoroutine(ShootAfterAnimation());
    }

    /// <summary>
    /// Waits for the throw animation to finish, then spawns the projectile and returns to Chase.
    /// </summary>
    private System.Collections.IEnumerator ShootAfterAnimation()
    {
        // Get animation duration; fall back to a small delay if not found
        float animDuration = GetAnimationDuration(shootAnimationName);
        if (animDuration <= 0f)
        {
            animDuration = 0.5f;
        }

        float waitTime = animDuration * spawnTimePercent;
        yield return new WaitForSeconds(waitTime);

        // If state changed while waiting, abort
        if (!isShooting || killer == null || killer.CurrentState != EnemyState.Shooting)
        {
            shootingCoroutine = null;
            yield break;
        }

        SpawnProjectile(pendingTargetPosition);

        lastShotTime = Time.time;
        isShooting = false;
        shootingCoroutine = null;

        if (killer != null && killer.CurrentState == EnemyState.Shooting)
        {
            killer.ChangeState(EnemyState.Chase);
        }
    }

    /// <summary>
    /// Actually spawns the projectile and sends it towards the cached target position.
    /// </summary>
    private void SpawnProjectile(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectileModule] Cannot spawn projectile - prefab is null.");
            return;
        }

        if (firePoint == null)
        {
            firePoint = killer != null ? killer.transform : transform;
        }

        // Instantiate projectile at fire point
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Calculate direction to target, but keep it horizontal so shots travel in a straight (parallel) line
        Vector3 flatTarget = targetPosition;
        flatTarget.y = firePoint.position.y; // ignore player feet height and aim straight from fire point
        Vector3 directionToTarget = (flatTarget - firePoint.position).normalized;

        // Apply velocity to projectile - ensure it has a Rigidbody
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        rb.linearVelocity = directionToTarget * projectileSpeed;

        // Rotate projectile to face direction of travel
        if (directionToTarget != Vector3.zero)
        {
            projectile.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }

        float animDuration = GetAnimationDuration(shootAnimationName);
        Debug.Log($"[ProjectileModule] Spawned projectile towards target. Distance: {Vector3.Distance(killer.transform.position, targetPosition)}, Animation duration: {animDuration}s");
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

#if UNITY_EDITOR
    /// <summary>
    /// Draws the projectile firing range as a gizmo in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Use killer's attack range as fallback when running in editor and not initialized yet
        float range = firingRange;
        if (killer != null && range <= 0f)
        {
            range = killer.AttackRange;
        }

        if (range <= 0f)
            return;

        // Center the gizmo on the killer if available, otherwise on this transform
        Vector3 center = killer != null ? killer.transform.position : transform.position;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f); // light cyan
        Gizmos.DrawWireSphere(center, range);

        // Also draw a line from the firePoint if set
        if (firePoint != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawLine(firePoint.position, center + (firePoint.forward * range));
        }
    }
#endif
}