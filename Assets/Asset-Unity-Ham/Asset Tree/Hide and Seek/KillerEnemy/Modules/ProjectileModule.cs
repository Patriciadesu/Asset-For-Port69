using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Projectile attack module for the Killer AI.
/// Fires projectiles at the player when within firing range.
/// Inherits attack range and damage from KillerAI base parameters.
/// </summary>
public class ProjectileModule : EnemyModule
{
    [Header("Projectile Settings")]
    [Tooltip("Transform where projectiles spawn from (e.g., hand or weapon point)")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Launch speed applied to every projectile (m/s).")]
    [SerializeField] private float projectileSpeed = 12f;

    [Tooltip("Enable ballistic arcs that rely on gravity. Disable for hitscan-like shots.")]
    [SerializeField] private bool useGravity = false;

    [Tooltip("Range at which this module will attempt to fire (uses killer.AttackRange if 0)")]
    [SerializeField] private float firingRange = 0f;

    [Tooltip("Animation state name to trigger for projectile attack")]
    [SerializeField] private string shootAnimationName = "Demon|Shoot1";

    [Tooltip("Minimum time between projectile shots")]
    [SerializeField] private float shootCooldown = 1.5f;

    [Header("Chance Settings")]
    [Tooltip("Projectiles trigger from the chase state using these random rolls.")]
    [SerializeField]
    private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.4f,
        Interval = new Vector2(1.5f, 3.5f),
        InitialDelay = new Vector2(0.25f, 0.75f)
    };

    [Header("Visuals")]
    [ShowAssetPreview]
    [SerializeField] private Material projectileMaterial;

    [Tooltip("Diameter of the generated projectile sphere (meters).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float projectileDiameter = 0.5f;

    [Tooltip("If true, the module waits for the shoot animation to fully finish before resuming movement.")]
    [SerializeField] private bool waitForAnimationEnd = true;

    private Animator animator;
    private static Material fallbackProjectileMaterial;
    private float lastShotTime = -10f;
    private bool isFiringAnimationPlaying = false;
    private float fireAnimationEndTime = 0f;
    private string shootAnimationShortName;
    private EnemyState? stateToRestore;
    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        animator = killer.GetAnimator();
        shootAnimationShortName = ExtractClipShortName(shootAnimationName);

        // If firePoint not assigned, use killer's transform
        if (firePoint == null)
        {
            firePoint = killer.transform;
        }

        AutoAssignHandFirePoint();

        chaseTrigger?.Prime();
        Debug.Log("[ProjectileModule] Initialized with firing range: " + (firingRange > 0 ? firingRange.ToString() : killer.AttackRange.ToString()));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoAssignHandFirePoint();
        }

        shootAnimationShortName = ExtractClipShortName(shootAnimationName);
    }
#endif

    public override void OnStateUpdate(EnemyState currentState)
    {
        HandleFireAnimationLock(currentState);
        if (isFiringAnimationPlaying)
            return;

        // Only attempt to shoot during Chase state (not Attack state, as that's reserved for melee)
        if (currentState != EnemyState.Chase)
            return;

        if (Time.time < lastShotTime + shootCooldown)
            return;

        Player target = GetTargetPlayer();
        if (target == null)
            return;

        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(killer.transform.position, target.transform.position);

        // Determine effective firing range
        float effectiveFiringRange = firingRange > 0 ? firingRange : killer.AttackRange;

        // Check if target is in range and cooldown has passed
        chaseTrigger?.PrimeIfNeeded();
        bool shouldFire = chaseTrigger != null && chaseTrigger.TryConsumeTrigger();

        if (shouldFire && distanceToTarget <= effectiveFiringRange)
        {
            FireProjectile(target.transform.position);
            lastShotTime = Time.time;
            chaseTrigger.BlockFor(shootCooldown);
        }
    }

    /// <summary>
    /// Fires a projectile towards the target position
    /// </summary>
    private void FireProjectile(Vector3 targetPosition)
    {
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

        GameObject projectile = CreateProjectileSphere();

        if (!projectile) return;

        Vector3 aimPoint = GetAimPoint(targetPosition);

        // Calculate launch velocity to reach the player
        Vector3 launchVelocity = ComputeLaunchVelocity(aimPoint);
        Vector3 launchDirection = launchVelocity.sqrMagnitude > 0.0001f
            ? launchVelocity.normalized
            : (aimPoint - firePoint.position).normalized;

        // Apply velocity to projectile (if it has a Rigidbody)
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }
        rb.useGravity = useGravity;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);
#else
        rb.velocity = Vector3.zero;
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);
#endif

        // Rotate projectile to face direction of travel
        projectile.transform.rotation = Quaternion.LookRotation(launchDirection);

        // Get animation duration for stun timing
        float animDuration = Mathf.Max(0.1f, GetAnimationDuration(shootAnimationName));
        BeginFireAnimationLock(animDuration);
        Debug.Log($"[ProjectileModule] Fired projectile at target. Distance: {Vector3.Distance(killer.transform.position, aimPoint)}, Animation duration: {animDuration}s");
    }
    private Vector3 GetAimPoint(Vector3 fallbackTargetPosition)
    {
        Player player = Player.Instance;
        if (player == null)
        {
            return fallbackTargetPosition + Vector3.up * 0.9f;
        }

        Collider coll = player.GetComponentInChildren<Collider>();
        if (coll == null)
        {
            return player.transform.position + Vector3.up * 0.9f;
        }

        return coll.bounds.center;
    }


    private GameObject CreateProjectileSphere()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ProjectileModule_Sphere";
        sphere.transform.position = firePoint ? firePoint.position : transform.position;
        sphere.transform.localScale = Vector3.one * Mathf.Clamp(projectileDiameter, 0.05f, 2f);

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer)
        {
            Material matToApply = projectileMaterial;

            if (matToApply == null)
            {
                if (fallbackProjectileMaterial == null)
                {
                    fallbackProjectileMaterial = new Material(Shader.Find("Standard"))
                    {
                        color = Color.magenta
                    };
                    fallbackProjectileMaterial.EnableKeyword("_EMISSION");
                    fallbackProjectileMaterial.SetColor("_EmissionColor", Color.magenta * 0.6f);
                }

                matToApply = fallbackProjectileMaterial;
            }

            renderer.sharedMaterial = matToApply;
        }

        var collider = sphere.GetComponent<Collider>();
        collider.isTrigger = true;

        var damage = sphere.AddComponent<FallbackProjectileDamage>();
        damage.Initialize(killer ? killer.AttackDamage : 10f, 8f);
        return sphere;
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

    private Vector3 ComputeLaunchVelocity(Vector3 targetPosition)
    {
        Vector3 origin = firePoint ? firePoint.position : transform.position;
        Vector3 toTarget = targetPosition - origin;

        if (!useGravity)
        {
            return toTarget.normalized * projectileSpeed;
        }

        Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = horizontal.magnitude;
        float height = toTarget.y;

        if (distance < 0.001f)
        {
            // Target is almost directly above/below – just shoot straight
            return toTarget.normalized * projectileSpeed;
        }

        float g = Mathf.Abs(Physics.gravity.y);
        float v2 = projectileSpeed * projectileSpeed;
        float underRoot = v2 * v2 - g * (g * distance * distance + 2f * height * v2);

        if (underRoot <= 0f)
        {
            // Not enough speed to hit target under gravity – fallback to straight shot
            return toTarget.normalized * projectileSpeed;
        }

        float root = Mathf.Sqrt(underRoot);
        float angle = Mathf.Atan((v2 + root) / (g * distance)); // high arc

        Vector3 dir = horizontal.normalized;
        Vector3 velocity = dir * projectileSpeed * Mathf.Cos(angle);
        velocity.y = projectileSpeed * Mathf.Sin(angle);
        return velocity;
    }

    private void BeginFireAnimationLock(float duration)
    {
        if (killer == null)
            return;

        stateToRestore = killer.CurrentState;
        if (killer.CurrentState != EnemyState.Idle)
        {
            killer.ChangeState(EnemyState.Idle);
        }

        killer.StopMovement();
        isFiringAnimationPlaying = true;
        fireAnimationEndTime = Time.time + duration;
    }

    private void HandleFireAnimationLock(EnemyState currentState)
    {
        if (!isFiringAnimationPlaying)
            return;

        if (HasAnimationFinished() && Time.time >= fireAnimationEndTime)
        {
            isFiringAnimationPlaying = false;
            if (killer != null)
            {
                float resumeSpeed = currentState == EnemyState.Chase ? killer.ChaseSpeed : killer.PatrolSpeed;
                killer.ResumeMovement(resumeSpeed);
                if (stateToRestore.HasValue && killer.CurrentState == EnemyState.Idle)
                {
                    killer.ChangeState(stateToRestore.Value);
                }
            }
            stateToRestore = null;
        }
    }

    private bool HasAnimationFinished()
    {
        if (!waitForAnimationEnd || animator == null)
        {
            return true;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        bool isInShootState =
            info.IsName(shootAnimationName) ||
            (!string.IsNullOrEmpty(shootAnimationShortName) && info.IsName(shootAnimationShortName));

        if (!isInShootState)
        {
            // Animation already switched out.
            return true;
        }

        return info.normalizedTime >= 0.98f;
    }

    private string ExtractClipShortName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return null;

        return clipName.Contains("|") ? clipName.Split('|')[1] : clipName;
    }

    private void AutoAssignHandFirePoint()
    {
        if (firePoint != null && firePoint.CompareTag("Hand"))
            return;

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (var child in transforms)
        {
            if (child == transform)
                continue;

            if (child.CompareTag("Hand"))
            {
                firePoint = child;
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        float range = firingRange > 0 ? firingRange : (killer != null ? killer.AttackRange : 2f);
        Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.65f);
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Gizmos.DrawWireSphere(origin, range);
    }

    [DisallowMultipleComponent]
    private sealed class FallbackProjectileDamage : MonoBehaviour
    {
        private float damageAmount;

        public void Initialize(float damage, float lifetime)
        {
            damageAmount = damage;
            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Player")
                DealDamage();
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Player")
                DealDamage();
        }

        private void DealDamage()
        {
            var player = Player.Instance;
            if (player != null && player.Stat != null)
            {
                player.Stat.TakeDamage(damageAmount);
            }
            Destroy(gameObject);
        }
    }
}
