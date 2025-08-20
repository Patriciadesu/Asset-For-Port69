using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    public BossStateGraph stateGraph;
    public Collider[] attackCollider;

    private bool hasPatrolState => stateGraph != null && stateGraph.transitionNodes != null &&
                                   stateGraph.transitionNodes.Any(t => t.nextStates != null &&
                                                                       t.nextStates.Any(s => s != null && s.state is BossPatrolState));
    [ShowIf("hasPatrolState")] public Transform[] waypoints;

    [HideInInspector] public UnityEvent onStateChanged;
    [HideInInspector] public UnityEvent<float> onStateTimeChanged;
    [HideInInspector] public UnityEvent<float> onHealthChanged;
    [HideInInspector] public UnityEvent onPlayerInSight;
    [HideInInspector] public UnityEvent onPlayerOutOfSight;
    [HideInInspector] public UnityEvent onPlayerInAttackRange;
    [HideInInspector] public UnityEvent onPlayerOutOfAttackRange;
    [HideInInspector] public UnityEvent onAttackEnd;

    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _health = 1f;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private float _sightRange = 10f;
    [SerializeField] private float _speed = 2f;

    // --- Added: player reference for distance checks (no triggers involved) ---
    [Header("Target (optional)")]
    [Tooltip("Assign player transform here. If left empty, the boss will find GameObject tagged 'Player'.")]
    [SerializeField] private Transform _player;

    private float _stateTime;
    private BossStateNode _lastStateNode;

    // Debounce flags for events
    private bool _isPlayerInSight;
    private bool _isPlayerInAttackRange;

    private const string PlayerTag = "Player";
    private float _reacquireTimer;

    private void Awake()
    {
        onStateChanged ??= new UnityEvent();
        onStateTimeChanged ??= new UnityEvent<float>();
        onHealthChanged ??= new UnityEvent<float>();
        onPlayerInSight ??= new UnityEvent();
        onPlayerOutOfSight ??= new UnityEvent();
        onPlayerInAttackRange ??= new UnityEvent();
        onPlayerOutOfAttackRange ??= new UnityEvent();
        onAttackEnd ??= new UnityEvent();

        // Ensure compound trigger events from child weapon colliders reach this Boss
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Weapon colliders: triggers, disabled by default (enabled in Attack state)
        if (attackCollider != null)
        {
            for (int i = 0; i < attackCollider.Length; i++)
            {
                var c = attackCollider[i];
                if (c == null) continue;
                c.isTrigger = true;
                c.enabled = false;
            }
        }
    }

    private void Start()
    {
        _player = Player.Instance.transform;
        if (stateGraph != null)
        {
            if (stateGraph.transitionNodes != null)
            {
                for (int i = 0; i < stateGraph.transitionNodes.Length; i++)
                {
                    var t = stateGraph.transitionNodes[i];
                    if (t != null) t.Bind(this);
                }
            }

            stateGraph.Awake();
            stateGraph.StartState();

            _lastStateNode = stateGraph.currentState;
            _stateTime = 0f;

            if (_lastStateNode != null)
                _lastStateNode.onStateChange.AddListener(OnGraphRequestedStateChange);
        }

        onHealthChanged.Invoke(_health);
    }

    private void OnDisable()
    {
        if (_lastStateNode != null)
            _lastStateNode.onStateChange.RemoveListener(OnGraphRequestedStateChange);
    }

    private void Update()
    {
        var g = stateGraph;
        if (g == null) return;

        if (g.currentState != _lastStateNode)
        {
            if (_lastStateNode != null)
                _lastStateNode.onStateChange.RemoveListener(OnGraphRequestedStateChange);

            _lastStateNode = g.currentState;
            _stateTime = 0f;
            onStateChanged.Invoke();

            if (_lastStateNode != null)
                _lastStateNode.onStateChange.AddListener(OnGraphRequestedStateChange);
        }

        var cs = g.currentState;
        if (cs != null)
        {
            switch (cs.state.stage)
            {
                case StateStage.Enter: cs.state.Enter(); break;
                case StateStage.Update: cs.state.Update(); break;
                    //case StateStage.Exit: cs.state.Exit(); break;
            }
            _stateTime += Time.deltaTime;
            onStateTimeChanged.Invoke(_stateTime);
        }
    }

    private void FixedUpdate()
    {
        var g = stateGraph;
        if (g != null && g.currentState != null && g.currentState.state.stage == StateStage.Update)
        {
            g.currentState.state.FixedUpdate();
        }

        // -------- Distance-based sight / attack-range checks (NO triggers) --------
        if (_player == null)
        {
            // Light reacquire once per 0.5s (avoids per-frame Find)
            _reacquireTimer -= Time.fixedDeltaTime;
            if (_reacquireTimer <= 0f)
            {
                _reacquireTimer = 0.5f;
            }
            // If still null, ensure we mark both false once
            if (_player == null)
            {
                SetSight(false);
                SetAttackRange(false);
                return;
            }
        }

        Vector3 to = _player.position - transform.position;
        float d2 = to.sqrMagnitude;

        float sight2 = _sightRange * _sightRange;
        float atk2 = _attackRange * _attackRange;

        SetSight(d2 <= sight2);
        SetAttackRange(d2 <= atk2);
    }

    private void OnGraphRequestedStateChange(BossStateNode _)
    {
        onStateChanged.Invoke();
        _stateTime = 0f;
    }

    // --------- Attack-only trigger: weapon collider hits player's collider -> TakeDamage ----------
    private void OnTriggerEnter(Collider other)
    {
        if (!IsInAttackState()) return;
        if (!other.CompareTag(PlayerTag)) return;

        float dmg = GetCurrentAttackDamage();
        if (dmg <= 0f) return;

        var targetGO = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        // Default simple signature; your Player can implement a different TakeDamage; SendMessage is flexible
        targetGO.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    // ---------------- Helpers ---------------

    private void SetSight(bool value)
    {
        if (_isPlayerInSight == value) return;
        _isPlayerInSight = value;
        if (value) onPlayerInSight.Invoke();
        else onPlayerOutOfSight.Invoke();
    }

    private void SetAttackRange(bool value)
    {
        if (_isPlayerInAttackRange == value) return;
        _isPlayerInAttackRange = value;
        if (value) onPlayerInAttackRange.Invoke();
        else onPlayerOutOfAttackRange.Invoke();
    }

    private bool IsInAttackState()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is BossAttackState;

    private float GetCurrentAttackDamage()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is BossAttackState a
           ? a.damage
           : 0f;

    // Signals to drive conditions from gameplay
    public void SetHealth(float newHealth) { _health = Mathf.Clamp(newHealth, 0f, _maxHealth); onHealthChanged.Invoke(_health); }
    public void PlayerInSight() { SetSight(true); }
    public void PlayerOutOfSight() { SetSight(false); }
    public void PlayerInAttackRange() { SetAttackRange(true); }
    public void PlayerOutOfAttackRange() { SetAttackRange(false); }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
