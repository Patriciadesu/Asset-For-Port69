using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    public BossStateGraph stateGraph;

    [HideInInspector] public UnityEvent onStateChanged;
    [HideInInspector] public UnityEvent<float> onStateTimeChanged;
    [HideInInspector] public UnityEvent<float> onHealthChanged;
    [HideInInspector] public UnityEvent onPlayerInSight;
    [HideInInspector] public UnityEvent onPlayerOutOfSight;
    [HideInInspector] public UnityEvent onPlayerInAttackRange;
    [HideInInspector] public UnityEvent onPlayerOutOfAttackRange;

    [SerializeField] private float _health = 1f;

    private float _stateTime;
    private BossStateNode _lastStateNode;

    private void Awake()
    {
        onStateChanged           ??= new UnityEvent();
        onStateTimeChanged       ??= new UnityEvent<float>();
        onHealthChanged          ??= new UnityEvent<float>();
        onPlayerInSight          ??= new UnityEvent();
        onPlayerOutOfSight       ??= new UnityEvent();
        onPlayerInAttackRange    ??= new UnityEvent();
        onPlayerOutOfAttackRange ??= new UnityEvent();
    }

    private void Start()
    {
        if (stateGraph != null)
        {
            // Bind transitions’ conditions to this boss (safe if Bind is no-op)
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
                case StateStage.Enter:  cs.state.Enter();  break;
                case StateStage.Update: cs.state.Update(); break;
                case StateStage.Exit:   cs.state.Exit();   break;
            }

            _stateTime += Time.deltaTime;
            onStateTimeChanged.Invoke(_stateTime);
        }
    }

    private void FixedUpdate()
    {
        var g = stateGraph;
        if (g != null && g.currentState != null && g.currentState.state.stage == StateStage.Update)
            g.currentState.state.FixedUpdate();
    }

    private void OnGraphRequestedStateChange(BossStateNode _)
    {
        onStateChanged.Invoke();
        _stateTime = 0f;
    }

    // Signals to drive conditions from gameplay
    public void SetHealth(float newHealth) { _health = newHealth; onHealthChanged.Invoke(_health); }
    public void PlayerInSight()            { onPlayerInSight.Invoke(); }
    public void PlayerOutOfSight()         { onPlayerOutOfSight.Invoke(); }
    public void PlayerInAttackRange()      { onPlayerInAttackRange.Invoke(); }
    public void PlayerOutOfAttackRange()   { onPlayerOutOfAttackRange.Invoke(); }
}
