using UnityEngine;

/// <summary>
/// Controls Demon character animations based on AI state.
/// Can work as a standalone component or integrate with the EnemyModule system.
/// Handles all animation transitions safely with proper null checks.
/// </summary>
public class DemonAnimationController : MonoBehaviour
{
    [Header("Animation Setup")]
    [Tooltip("Reference to the Animator component")]
    [SerializeField] private Animator animator;

    [Tooltip("Reference to the KillerAI (optional - will auto-find if null)")]
    [SerializeField] private KillerAI killerAI;

    [Header("Animation Settings")]
    [Tooltip("Smoothing time for animation transitions")]
    [SerializeField] private float transitionTime = 0.15f;

    [Tooltip("Use random idle animations")]
    [SerializeField] private bool randomizeIdle = true;

    [Tooltip("Use random attack animations")]
    [SerializeField] private bool randomizeAttack = true;

    [Header("Speed Blending")]
    [Tooltip("Enable speed-based animation blending")]
    [SerializeField] private bool useSpeedBlending = true;

    [Tooltip("Threshold speed to switch from walk to run")]
    [SerializeField] private float runSpeedThreshold = 4f;

    [Header("Looping")]
    [Tooltip("Auto-restart looping animations when they finish")]
    [SerializeField] private bool autoLoopAnimations = true;

    [Tooltip("Check interval for loop detection (seconds)")]
    [SerializeField] private float loopCheckInterval = 0.1f;

    // Primary animation names (canonical)
    private const string IDLE_1 = "Demon|Idle1";
    private const string IDLE_2 = "Demon|Idle2";
    private const string IDLE_3 = "Demon|Idle3";
    private const string WALK_1 = "Demon|Walk1";
    private const string WALK_2 = "Demon|Walk2";
    private const string RUN_1 = "Demon|Run1";
    private const string PUNCH_1 = "Demon|Punch1";
    private const string PUNCH_2 = "Demon|Punch2";
    private const string PUNCH_3 = "Demon|Punch3";
    private const string SHOOT_1 = "Demon|Shoot1";
    private const string SHOOT_2 = "Demon|Shoot2";
    private const string THROW = "Demon|Throw";
    private const string THROW_LOOP = "Demon|Throw-loop";
    private const string THROW_CATCH = "Demon|Throw-catch";
    private const string TELEPATHIC = "Demon|Telepathic";
    private const string TELEPATHIC_LOOP = "Demon|Telepathic-loop";
    private const string GET_DAMAGE = "Demon|Get-damage";
    private const string JUMP_LONG = "Demon|Jump-long";
    private const string JUMP_SHORT = "Demon|Jump-short";
    private const string TURN_RIGHT = "Demon|Turn-right";
    private const string TURN_LEFT = "Demon|Turn-left";
    private const string SPASM = "Demon|Spasm";
    private const string COME_OUT_2 = "Demon|Come-out2";

    // Alternate names to tolerate typos or different asset naming
    private static readonly string[] Idle1Aliases = { "Demon|ldlel", IDLE_1 };
    private static readonly string[] Idle2Aliases = { "Demon|ldle2", IDLE_2 };
    private static readonly string[] Idle3Aliases = { "Demon|ldle3", IDLE_3 };
    private static readonly string[] Punch1Aliases = { "Demon|punchl", PUNCH_1 };
    private static readonly string[] Shoot1Aliases = { "Demon|ShootI", SHOOT_1 };
    private static readonly string[] TelepathicAliases = { "Demon|TeIepathic", TELEPATHIC };

    // Animator parameters (if using blend trees)
    private const string PARAM_SPEED = "Speed";
    private const string PARAM_IS_MOVING = "IsMoving";
    private const string PARAM_ATTACK_INDEX = "AttackIndex";

    // State tracking
    private EnemyState currentState = EnemyState.Idle;
    private string currentAnimationName = "";
    private float currentSpeed = 0f;
    private float lastLoopCheckTime = 0f;

    // Module integration (optional)
    private EnemyModule attachedModule;
    private bool isModuleMode = false;

    private void Awake()
    {
        // Auto-find Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        // Auto-find KillerAI if not assigned
        if (killerAI == null)
        {
            killerAI = GetComponent<KillerAI>();
        }

        // Check if this is attached to a module
        attachedModule = GetComponent<EnemyModule>();
        isModuleMode = attachedModule != null;

        if (animator == null)
        {
            Debug.LogWarning("[DemonAnimationController] No Animator found! Animations will not play.");
        }
    }

    private void Start()
    {
        // Play initial idle animation
        if (animator != null)
        {
            PlayIdleAnimation();
        }
    }

    private void Update()
    {
        // Only update if we have required components
        if (killerAI == null || animator == null)
            return;

        // Check for state changes
        EnemyState newState = killerAI.CurrentState;
        if (newState != currentState)
        {
            OnStateChanged(newState);
            currentState = newState;
        }

        // Update speed-based blending
        if (useSpeedBlending)
        {
            UpdateSpeedBlending();
        }

        // Check if looping animations need to be restarted
        if (autoLoopAnimations && Time.time - lastLoopCheckTime >= loopCheckInterval)
        {
            lastLoopCheckTime = Time.time;
            CheckAndLoopAnimation();
        }
    }

    /// <summary>
    /// Called when the AI state changes
    /// </summary>
    private void OnStateChanged(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Idle:
                PlayIdleAnimation();
                break;

            case EnemyState.Patrol:
                PlayWalkAnimation();
                break;

            case EnemyState.Chase:
                PlayChaseAnimation();
                break;

            case EnemyState.Attack:
                PlayAttackAnimation();
                break;
        }
    }

    /// <summary>
    /// Update speed parameter for blend trees
    /// </summary>
    private void UpdateSpeedBlending()
    {
        if (animator == null || killerAI == null)
            return;

        // Get speed from NavMeshAgent if available
        var agent = killerAI.GetAgent();
        if (agent != null)
        {
            currentSpeed = agent.velocity.magnitude;
            
            // Set animator parameters if they exist
            if (HasParameter(PARAM_SPEED))
            {
                animator.SetFloat(PARAM_SPEED, currentSpeed);
            }

            if (HasParameter(PARAM_IS_MOVING))
            {
                animator.SetBool(PARAM_IS_MOVING, currentSpeed > 0.1f);
            }
        }
    }

    /// <summary>
    /// Check if current animation has finished and needs to loop
    /// </summary>
    private void CheckAndLoopAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(currentAnimationName))
            return;

        // Only loop animations that should be looping
        if (!IsLoopingAnimation(currentAnimationName))
            return;

        // Get current animation state
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Check if animation has completed (normalized time >= 1.0)
        // and is not set to loop in the Animator Controller
        if (stateInfo.normalizedTime >= 0.95f && !stateInfo.loop)
        {
            // Restart the animation
            animator.Play(currentAnimationName, 0, 0f);
        }
    }

    /// <summary>
    /// Check if an animation should loop (idle, walk, run, etc.)
    /// </summary>
    private bool IsLoopingAnimation(string animName)
    {
        if (string.IsNullOrEmpty(animName))
            return false;

        // Check if this is a looping animation type
        return animName.Contains("Idle") || animName.Contains("ldle") ||
               animName.Contains("Walk") ||
               animName.Contains("Run") ||
               animName.Contains("Telepathic-loop") ||
               animName.Contains("Throw-loop");
    }

    #region Animation Playback Methods

    /// <summary>
    /// Play an idle animation (random or sequential)
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (randomizeIdle)
        {
            int random = Random.Range(0, 3);
            switch (random)
            {
                case 0: PlayAny(Idle1Aliases); break;
                case 1: PlayAny(Idle2Aliases); break;
                case 2: PlayAny(Idle3Aliases); break;
            }
        }
        else
        {
            PlayAny(Idle1Aliases);
        }
    }

    /// <summary>
    /// Play walk animation
    /// </summary>
    public void PlayWalkAnimation()
    {
        // Alternate between walk animations or use based on speed
        if (currentSpeed < runSpeedThreshold)
        {
            PlayAnimation(Random.value > 0.5f ? WALK_1 : WALK_2);
        }
        else
        {
            PlayAnimation(RUN_1);
        }
    }

    /// <summary>
    /// Play chase/run animation
    /// </summary>
    public void PlayChaseAnimation()
    {
        PlayAnimation(RUN_1);
    }

    /// <summary>
    /// Play attack animation (random melee attack)
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (randomizeAttack)
        {
            int random = Random.Range(0, 3);
            switch (random)
            {
                case 0: PlayAny(Punch1Aliases); break;
                case 1: PlayAnimation(PUNCH_2); break;
                case 2: PlayAnimation(PUNCH_3); break;
            }
        }
        else
        {
            PlayAny(Punch1Aliases);
        }
    }

    /// <summary>
    /// Play a specific punch animation
    /// </summary>
    public void PlayPunchAnimation(int punchIndex)
    {
        switch (punchIndex)
        {
            case 1: PlayAny(Punch1Aliases); break;
            case 2: PlayAnimation(PUNCH_2); break;
            case 3: PlayAnimation(PUNCH_3); break;
            default: PlayAny(Punch1Aliases); break;
        }
    }

    /// <summary>
    /// Play shoot animation
    /// </summary>
    public void PlayShootAnimation(int shootIndex = 1)
    {
        if (shootIndex == 2)
            PlayAnimation(SHOOT_2);
        else
            PlayAny(Shoot1Aliases);
    }

    /// <summary>
    /// Play throw animation sequence
    /// </summary>
    public void PlayThrowAnimation()
    {
        PlayAnimation(THROW);
    }

    /// <summary>
    /// Play throw loop animation (for charging throw)
    /// </summary>
    public void PlayThrowLoopAnimation()
    {
        PlayAnimation(THROW_LOOP);
    }

    /// <summary>
    /// Play throw catch animation (catching thrown object)
    /// </summary>
    public void PlayThrowCatchAnimation()
    {
        PlayAnimation(THROW_CATCH);
    }

    /// <summary>
    /// Play telepathic ability animation
    /// </summary>
    public void PlayTelepathicAnimation()
    {
        PlayAny(TelepathicAliases);
    }

    /// <summary>
    /// Play telepathic loop animation
    /// </summary>
    public void PlayTelepathicLoopAnimation()
    {
        PlayAnimation(TELEPATHIC_LOOP);
    }

    /// <summary>
    /// Play damage reaction animation
    /// </summary>
    public void PlayDamageAnimation()
    {
        PlayAnimation(GET_DAMAGE);
    }

    /// <summary>
    /// Play jump animation
    /// </summary>
    public void PlayJumpAnimation(bool longJump = false)
    {
        if (longJump)
            PlayAnimation(JUMP_LONG);
        else
            PlayAnimation(JUMP_SHORT);
    }

    /// <summary>
    /// Play turn animation
    /// </summary>
    public void PlayTurnAnimation(bool turnRight)
    {
        if (turnRight)
            PlayAnimation(TURN_RIGHT);
        else
            PlayAnimation(TURN_LEFT);
    }

    /// <summary>
    /// Play spasm animation
    /// </summary>
    public void PlaySpasmAnimation()
    {
        PlayAnimation(SPASM);
    }

    /// <summary>
    /// Play come out animation
    /// </summary>
    public void PlayComeOutAnimation()
    {
        PlayAnimation(COME_OUT_2);
    }

    #endregion

    #region Core Animation Methods

    /// <summary>
    /// Try to play the first existing animation from candidates
    /// </summary>
    private void PlayAny(params string[] candidates)
    {
        string picked = ResolveAnimation(candidates);
        if (!string.IsNullOrEmpty(picked))
        {
            PlayAnimation(picked);
        }
        else
        {
            // Fall back to first candidate even if not present to keep flow consistent
            if (candidates != null && candidates.Length > 0)
                PlayAnimation(candidates[0]);
        }
    }

    /// <summary>
    /// Returns the first candidate that exists in the Animator
    /// </summary>
    private string ResolveAnimation(params string[] candidates)
    {
        if (animator == null || candidates == null)
            return null;

        foreach (var c in candidates)
        {
            if (!string.IsNullOrEmpty(c) && HasAnimationState(c))
                return c;
        }
        return null;
    }

    /// <summary>
    /// Play an animation by name with crossfade
    /// </summary>
    private void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName))
            return;

        // Don't replay the same animation
        if (currentAnimationName == animationName)
            return;

        // Check if animation state exists before playing
        if (HasAnimationState(animationName))
        {
            animator.CrossFade(animationName, transitionTime);
            currentAnimationName = animationName;
        }
        else
        {
            Debug.LogWarning($"[DemonAnimationController] Animation state '{animationName}' not found in Animator!");
        }
    }

    /// <summary>
    /// Play animation immediately without crossfade
    /// </summary>
    public void PlayAnimationImmediate(string animationName)
    {
        if (animator == null)
            return;

        if (HasAnimationState(animationName))
        {
            animator.Play(animationName, 0, 0f);
            currentAnimationName = animationName;
        }
        else
        {
            Debug.LogWarning($"[DemonAnimationController] Animation state '{animationName}' not found in Animator!");
        }
    }

    /// <summary>
    /// Check if the animator has a specific state
    /// </summary>
    private bool HasAnimationState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        // Check all layers for the state
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(stateName)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if animator has a specific parameter
    /// </summary>
    private bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Get current animation name
    /// </summary>
    public string GetCurrentAnimation()
    {
        return currentAnimationName;
    }

    /// <summary>
    /// Check if an animation is currently playing
    /// </summary>
    public bool IsAnimationPlaying(string animationName)
    {
        if (animator == null)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }

    /// <summary>
    /// Get normalized time of current animation (0-1)
    /// </summary>
    public float GetAnimationNormalizedTime()
    {
        if (animator == null)
            return 0f;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime;
    }

    /// <summary>
    /// Set animation speed
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    #endregion

    #region Module Integration (Optional)

    /// <summary>
    /// This can be called by modules during OnStateEnter to trigger specific animations
    /// </summary>
    public void OnModuleStateEnter(EnemyState state, string customAnimation = "")
    {
        if (!string.IsNullOrEmpty(customAnimation))
        {
            PlayAny(customAnimation);
        }
        else
        {
            OnStateChanged(state);
        }
    }

    /// <summary>
    /// This can be called by modules during OnStateUpdate
    /// </summary>
    public void OnModuleStateUpdate(EnemyState state)
    {
        // Modules can call public animation methods directly
        // This is just a hook for future functionality
    }

    /// <summary>
    /// This can be called by modules during OnStateExit
    /// </summary>
    public void OnModuleStateExit(EnemyState state)
    {
        // Hook for future functionality
    }

    #endregion
}
