using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public partial class Player : Singleton<Player>
{

    [BoxGroup("Player Camera"), SerializeReference]
    public CameraModule Cam = null;

    [BoxGroup("Player Movement"), SerializeReference]
    public MovementModule Movement = null;
    [BoxGroup("Player Stat"), SerializeReference]
    public StatModule Stat = null;
    #region Player Properties

    #region Camera Settings
    [Foldout("DO NOT TOUCH")] public Camera camera;
    [Foldout("DO NOT TOUCH")] public Transform fpsCameraPivot;
    [Foldout("DO NOT TOUCH")] public Camera tpsCamera;
    [Foldout("DO NOT TOUCH")] public CinemachineThirdPersonFollow tpsVirtualCamera;
    [Foldout("DO NOT TOUCH")] public Transform tpsCameraPivot;
    #endregion


    #region Components
    [HideInInspector] public Rigidbody rigidbody;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider capsuleCollider;
    #endregion

    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;


    [HideInInspector] public bool canRotateCamera = true;

    #region Player Delegates
    public delegate void StartDelegate();
    public StartDelegate onStart;
    public delegate void OnValidateDelegate();
    public OnValidateDelegate onValidate;
    public delegate void FixedUpdateDelegate();
    public FixedUpdateDelegate onFixedUpdate;
    public delegate void UpdateDelegate();
    public UpdateDelegate onUpdate;
    public delegate void CollisionEnterDelegate(Collision collision);
    public CollisionEnterDelegate onCollisionEnter;
    public delegate void CollisionStayDelegate(Collision collision);
    public CollisionStayDelegate onCollisionStay;
    public delegate void CollisionExitDelegate(Collision collision);
    public CollisionExitDelegate onCollisionExit;
    public delegate void TriggerEnterDelegate(Collider other);
    public TriggerEnterDelegate onTriggerEnter;
    public delegate void TriggerStayDelegate(Collider other);
    public TriggerStayDelegate onTriggerStay;
    public delegate void TriggerExitDelegate(Collider other);
    public TriggerExitDelegate onTriggerExit;

    #endregion

    // Extensions
    private PlayerExtension[] extensions;

    #endregion
    #region Interface Components
    [HideInInspector] public List<ICancleGravity> cancleGravityComponents = new List<ICancleGravity>();
    [HideInInspector] public bool canApplyGravity => cancleGravityComponents.TrueForAll(x => x.canApplyGravity);
    [HideInInspector] public List<IInteruptPlayerMovement> interuptPlayerMovementComponents = new List<IInteruptPlayerMovement>();
    [HideInInspector] public bool canMove => !interuptPlayerMovementComponents.Any(x => x.isPerforming) && !isStunned;
    [HideInInspector] public List<IUseStamina> staminaComponentStates = new List<IUseStamina>();
    [HideInInspector] public bool canGenerateStamina => staminaComponentStates.TrueForAll(x => !x.isUsingStamina);
    #endregion
    
    #region Stun System
    [HideInInspector] public bool isStunned { get; private set; }
    private Coroutine stunCoroutine;
    #endregion

    #region Unity Methods
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (Cam == null) Cam = new CameraModule(this);
        if (Movement == null) Movement = new MovementModule(this);
        if (Stat == null) Stat = new StatModule(this);

        // 🔧 Bind immediately so modules can work even in Edit Mode previews
        Cam.Bind(this);
        Movement.Bind(this);
        Stat.Bind(this);
    }

    void Start()
    {
        if (!Application.isPlaying) return;

        // Gather extensions but defer their OnStart until after player/modules are ready
        SetExtensions();
        SetSpawnPoint(transform.position);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Bind modules once
        Stat.Bind(this);
        Cam.Bind(this);
        Movement.Bind(this);

        // Fire player start
        onStart?.Invoke();

        // Let extensions know the player is fully initialized
        foreach (var ext in extensions)
        {
            ext.OnStart(this);

        }
        if (Inventory.Instance == null)
        {
            this.AddComponent<Inventory>();
        }
        // Populate interface-component lists
        RefreshInterfaceComponents();
    }

#if UNITY_EDITOR
void OnValidate()
{
    // Keep cached component refs fresh in Edit Mode
    rigidbody = GetComponent<Rigidbody>();
    animator = GetComponent<Animator>();
    capsuleCollider = GetComponent<CapsuleCollider>();

    // Ensure modules exist
    if (Cam == null)      Cam      = new CameraModule(this);
    if (Movement == null) Movement = new MovementModule(this);
    if (Stat == null)     Stat     = new StatModule(this);

    // 🔧 IMPORTANT: Re-bind so 'player' back-reference is valid in Edit Mode
    Cam.Bind(this);
    Movement.Bind(this);
    Stat.Bind(this);

    // Forward validation
    Cam.OnValidate();
    Movement.OnValidate();
    Stat.OnValidate();
}
#endif


    void Update()
    {
        onUpdate?.Invoke();
    }

    void FixedUpdate() => onFixedUpdate?.Invoke();

    void OnCollisionEnter(Collision c) { if (Application.isPlaying) onCollisionEnter?.Invoke(c); }
    void OnCollisionStay(Collision c) { if (Application.isPlaying) onCollisionStay?.Invoke(c); }
    void OnCollisionExit(Collision c) { if (Application.isPlaying) onCollisionExit?.Invoke(c); }
    void OnTriggerEnter(Collider o) { if (Application.isPlaying) onTriggerEnter?.Invoke(o); }
    void OnTriggerStay(Collider o) { if (Application.isPlaying) onTriggerStay?.Invoke(o); }
    void OnTriggerExit(Collider o) { if (Application.isPlaying) onTriggerExit?.Invoke(o); }

    // ——— helpers ———
    void RefreshInterfaceComponents()
    {
        interuptPlayerMovementComponents.Clear();
        cancleGravityComponents.Clear();
        staminaComponentStates.Clear();

        var ownedComponents = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var component in ownedComponents)
        {
            if (component is IInteruptPlayerMovement movementGate)
                interuptPlayerMovementComponents.Add(movementGate);

            if (component is ICancleGravity gravityGate)
                cancleGravityComponents.Add(gravityGate);

            if (component is IUseStamina staminaUser)
                staminaComponentStates.Add(staminaUser);
        }
    }
    #endregion

    #region Player Methods

    #region Setup Methods

    public void SetExtensions()
    {
        extensions = GetComponents<PlayerExtension>();
        foreach (var extension in extensions)
        {
            extension.OnStart(this);
        }
    }


    public void SetSpawnPoint(Vector3 spawnPoint)
    {
        this.spawnPoint = spawnPoint;
    }


    #endregion



    public void Respawn()
    {
        rigidbody.linearVelocity = Vector3.zero;
        Debug.Log("Respawning");
        if (lastCheckpoint == Vector3.zero)
        {
            Debug.Log("Last Checkpoint is null");
            this.transform.position = spawnPoint;
        }
        else this.transform.position = lastCheckpoint;

        Stat.currenthealth = Stat.maxhealth;
    }
    //Done
    public float GetAnimationLength(string animationName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 0f;
    }
    
    /// <summary>
    /// Applies a stun effect for the specified duration
    /// </summary>
    /// <param name="duration">Duration of the stun in seconds</param>
    public void ApplyStun(float duration)
    {
        // If already stunned, restart the stun
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        Debug.Log($"[Player] Stunned for {duration} seconds");
        
        yield return new WaitForSeconds(duration);
        
        isStunned = false;
        stunCoroutine = null;
        Debug.Log("[Player] Stun ended");
    }
    
    /// <summary>
    /// Manually end the stun early
    /// </summary>
    public void EndStun()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
        
        isStunned = false;
    }

    #endregion

    #region Gizmos
    //Done


    #endregion

}
public enum CameraType
{
    FirstPerson,
    ThirdPerson
}