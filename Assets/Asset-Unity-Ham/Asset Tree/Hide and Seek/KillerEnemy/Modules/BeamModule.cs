using System.Collections;
using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Beam Module")]
[Tooltip("Hitscan beam that deals damage to the player.")]
public class BeamModule : EnemyModule
{
    [Header("Beam Settings")]
    [Tooltip("Maximum range of the beam (uses KillerAI.AttackRange if 0)")]
    public float Range = 20f;

    [Tooltip("Amount of damage dealt by the beam")]
    public float BeamDamage = 20f;

    [Tooltip("Minimum time between beam shots")]
    public float BeamCooldown = 3f;

    [Tooltip("Transform from which the beam originates (e.g., hand or head). If null, uses killer transform.")]
    public Transform BeamOrigin;

    [Tooltip("Layer mask for beam raycasting")]
    public LayerMask HitLayers = ~0; // All layers by default

    [Header("Beam Visuals")]
    [Tooltip("Optional beam effect prefab (e.g., blue line). If null, a LineRenderer will be created.")]
    public GameObject BeamEffectPrefab;

    [Tooltip("Beam color when using built-in LineRenderer effect")]
    public Color BeamColor = Color.cyan;

    [Tooltip("Base line width for the built-in LineRenderer effect")]
    public float BeamWidth = 0.05f;

    [Tooltip("Duration in seconds that the beam visual stays visible")]
    public float BeamEffectDuration = 0.15f;

    [Header("Bolt/Zigzag Style")]
    [Tooltip("If true, the fallback line effect will use a zigzag lightning-style bolt instead of a straight beam.")]
    public bool UseZigZag = false;

    [Tooltip("Number of segments used for the zigzag bolt (higher = smoother)")]
    [Range(2, 64)] public int ZigZagSegments = 12;

    [Tooltip("Maximum sideways offset for the zigzag (world units)")]
    public float ZigZagAmplitude = 0.25f;

    [Header("Animation")]
    [Tooltip("Animation state name used for the beam attack timing")]
    public string BeamAnimationName = "Demon|Shoot2";

    [Tooltip("Fraction (0-1) of the animation after which the beam fires")]
    [Range(0.1f, 1f)]
    public float FireTimePercent = 1f;

    private Animator animator;
    private float lastBeamTime = -10f;
    private bool isBeaming = false;      // true while performing a beam attack in Shooting state
    private Vector3 pendingTargetPosition;
    private Coroutine beamCoroutine;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        animator = killer.GetAnimator();

        if (BeamOrigin == null && killer != null)
        {
            // Prefer EnemyHand marker if present; otherwise use the killer's transform
            EnemyHand hand = killer.GetComponentInChildren<EnemyHand>();
            if (hand != null)
            {
                BeamOrigin = hand.transform;
            }
            else
            {
                BeamOrigin = killer.transform;
            }
        }
    }

    private void OnDisable()
    {
        if (beamCoroutine != null)
        {
            StopCoroutine(beamCoroutine);
            beamCoroutine = null;
        }
        isBeaming = false;
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null)
            return;

        Player target = GetTargetPlayer();
        if (target == null)
            return;

        // Distance to target
        float distanceToTarget = Vector3.Distance(killer.transform.position, target.transform.position);

        // Effective range (fallback to KillerAI.AttackRange if Range <= 0)
        float effectiveRange = Range > 0 ? Range : killer.AttackRange;

        if (currentState == EnemyState.Chase)
        {
            // Start a new beam attack from Chase, just like the projectile module
            if (!isBeaming &&
                distanceToTarget <= effectiveRange &&
                Time.time >= lastBeamTime + BeamCooldown)
            {
                pendingTargetPosition = target.transform.position;
                isBeaming = true;

                // Enter the Shooting state used for ranged attacks
                killer.ChangeState(EnemyState.Shooting);
            }

            return;
        }

        // While in Shooting state, keep looking at the target for better visuals
        if (currentState == EnemyState.Shooting && isBeaming)
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

        // When we enter Shooting because of this module, start the beam animation
        if (newState == EnemyState.Shooting && isBeaming)
        {
            BeginBeam(pendingTargetPosition);
        }
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (oldState == EnemyState.Shooting)
        {
            if (beamCoroutine != null)
            {
                StopCoroutine(beamCoroutine);
                beamCoroutine = null;
            }

            isBeaming = false;
        }
    }

    /// <summary>
    /// Starts the beam process: play animation; beam is fired at the end of the animation.
    /// </summary>
    private void BeginBeam(Vector3 targetPosition)
    {
        if (animator == null)
        {
            Debug.LogWarning("[BeamModule] Animator reference is null, firing beam immediately without animation.");
            FireBeam(targetPosition);
            lastBeamTime = Time.time;
            isBeaming = false;

            if (killer != null && killer.CurrentState == EnemyState.Shooting)
            {
                killer.ChangeState(EnemyState.Chase);
            }
            return;
        }

        // Stop movement while we perform the beam animation
        killer.StopMovement();

        // Optionally trigger a "Shoot" trigger if present (same as projectile module)
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
            Debug.Log($"[BeamModule] Playing beam animation via 'Shoot' trigger: {BeamAnimationName}");
        }
        else
        {
            Debug.LogWarning("[BeamModule] Animator does not have 'Shoot' trigger parameter! Beam will still be fired using timing.");
        }

        pendingTargetPosition = targetPosition;

        if (beamCoroutine != null)
        {
            StopCoroutine(beamCoroutine);
        }
        beamCoroutine = StartCoroutine(FireBeamAfterAnimation());
    }

    /// <summary>
    /// Waits for the beam animation to reach the fire point, then fires the beam and returns to Chase.
    /// </summary>
    private IEnumerator FireBeamAfterAnimation()
    {
        float animDuration = GetAnimationDuration(BeamAnimationName);
        if (animDuration <= 0f)
        {
            animDuration = 0.5f;
        }

        float waitTime = animDuration * FireTimePercent;
        yield return new WaitForSeconds(waitTime);

        if (!isBeaming || killer == null || killer.CurrentState != EnemyState.Shooting)
        {
            beamCoroutine = null;
            yield break;
        }

        FireBeam(pendingTargetPosition);

        lastBeamTime = Time.time;
        isBeaming = false;
        beamCoroutine = null;

        if (killer != null && killer.CurrentState == EnemyState.Shooting)
        {
            killer.ChangeState(EnemyState.Chase);
        }
    }

    /// <summary>
    /// Actually casts the beam, damages the player, and spawns a visual effect.
    /// </summary>
    private void FireBeam(Vector3 targetPosition)
    {
        if (BeamOrigin == null)
        {
            BeamOrigin = killer != null ? killer.transform : transform;
        }

        Vector3 origin = BeamOrigin.position;

        // Aim horizontally towards the player so the beam travels in a straight line
        Vector3 flatTarget = targetPosition;
        flatTarget.y = origin.y;
        Vector3 dir = (flatTarget - origin).normalized;

        float maxDistance = Range > 0 ? Range : killer.AttackRange;

        // Raycast to detect hits
        bool hitSomething = Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, HitLayers);

        Vector3 endPoint = hitSomething ? hit.point : origin + dir * maxDistance;

        // Visual debug line
        Debug.DrawLine(origin, endPoint, BeamColor, BeamEffectDuration);

        // Spawn visual beam effect between origin and end point
        SpawnBeamEffect(origin, endPoint);

        if (hitSomething)
        {
            // Only damage the player if we actually hit the player object (by tag)
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                Player player = hit.collider.GetComponent<Player>();
                if (player == null)
                {
                    player = Player.Instance;
                }

                if (player != null && player.Stat != null)
                {
                    player.Stat.TakeDamage(BeamDamage);
                    Debug.Log($"[BeamModule] Beam hit player for {BeamDamage} damage!");
                }
            }
            else
            {
                Debug.Log("[BeamModule] Beam hit something but not the player");
            }
        }
        else
        {
            Debug.Log("[BeamModule] Beam missed");
        }
    }

    /// <summary>
    /// Spawns a simple beam effect between origin and endpoint.
    /// If BeamEffectPrefab is set, it will be instantiated and oriented along the beam.
    /// Otherwise, a temporary LineRenderer will be created.
    /// </summary>
    private void SpawnBeamEffect(Vector3 origin, Vector3 endPoint)
    {
        float distance = Vector3.Distance(origin, endPoint);
        if (distance <= 0.01f)
            return;

        if (BeamEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(BeamEffectPrefab, origin, Quaternion.LookRotation(endPoint - origin));

            // Try to scale the effect along its forward axis to match the distance
            effect.transform.localScale = new Vector3(effect.transform.localScale.x, effect.transform.localScale.y, distance);

            Object.Destroy(effect, BeamEffectDuration);
        }
        else
        {
            // Fallback: LineRenderer beam or zigzag bolt with configurable width and style
            GameObject go = new GameObject("BeamEffectTemp");
            LineRenderer lr = go.AddComponent<LineRenderer>();

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = BeamColor;
            lr.endColor = BeamColor;
            lr.useWorldSpace = true;

            lr.startWidth = BeamWidth;
            lr.endWidth = BeamWidth;

            if (UseZigZag && ZigZagSegments >= 2)
            {
                int segments = Mathf.Max(2, ZigZagSegments);
                lr.positionCount = segments + 1;

                Vector3 direction = (endPoint - origin).normalized;
                float step = distance / segments;

                // Choose a perpendicular axis for jitter
                Vector3 perp = Vector3.Cross(direction, Vector3.up);
                if (perp.sqrMagnitude < 0.001f)
                {
                    perp = Vector3.Cross(direction, Vector3.right);
                }
                perp.Normalize();

                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 basePos = origin + direction * (t * distance);

                    // Randomized offset along perpendicular for bolt look
                    float offset = (i == 0 || i == segments) ? 0f : Random.Range(-ZigZagAmplitude, ZigZagAmplitude);
                    Vector3 jitter = perp * offset;

                    lr.SetPosition(i, basePos + jitter);
                }
            }
            else
            {
                // Simple straight beam
                lr.positionCount = 2;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, endPoint);
            }

            Object.Destroy(go, BeamEffectDuration);
        }
    }

    /// <summary>
    /// Gets the duration of a specific animation clip by name.
    /// </summary>
    private float GetAnimationDuration(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f;

        if (string.IsNullOrEmpty(animationName))
            return 0.5f;

        string shortName = animationName.Contains("|") ? animationName.Split('|')[1] : animationName;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == shortName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning($"[BeamModule] Animation clip '{shortName}' not found in animator!");
        return 0.5f;
    }
}
