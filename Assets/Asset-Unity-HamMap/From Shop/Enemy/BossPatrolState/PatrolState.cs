using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;
using NaughtyAttributes;
using System.Linq;


[System.Serializable]

public class PatrolState : BossState
{
    public Transform[] waypoints => boss.waypoints;
    private float moveSpeed = 2f;

    private float arriveThreshold = 0.2f;

    private bool useNavMeshIfAvailable = true;

    public float idleTime = 2f;

    private int _index;
    private Transform _self;
    private NavMeshAgent _agent;

    private float _idleTimer;
    private bool _isWaiting;

    public PatrolState(Boss bossInstance) : base("Patrol", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _agent = boss != null ? boss.GetComponent<NavMeshAgent>() : null;
    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetBool("Walk", true);

        _isWaiting = false;
        _idleTimer = 0f;

        if (_self == null) return;

        // Use nearest point as starting index (simple quality-of-life)
        if (waypoints != null && waypoints.Length > 0)
        {
            _index = 0;
            float best = float.PositiveInfinity;
            var pos = _self.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var w = waypoints[i];
                if (w == null) continue;
                float d = (w.transform.position - pos).sqrMagnitude;
                if (d < best) { best = d; _index = i; }
            }

            if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = Mathf.Max(0.01f, moveSpeed);
                _agent.SetDestination(waypoints[_index].transform.position);
            }
        }
    }
    public override void Update()
    {
        if (_self == null || waypoints == null || waypoints.Length == 0)
        {
            base.Update();
            return;
        }

        // Handle idle waiting
        if (_isWaiting)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                _isWaiting = false;
                NextWaypoint();
            }
            return; // skip movement while waiting
        }

        var target = waypoints[_index];
        if (target == null) { base.Update(); return; }

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            // Agent handles movement; check arrival
            if (!_agent.pathPending && _agent.remainingDistance <= Mathf.Max(0.05f, arriveThreshold))
            {
                StartIdle();
            }
        }
        else
        {
            // Transform-based movement
            Vector3 dir = (target.transform.position - _self.position);
            float dist = dir.magnitude;
            if (dist <= arriveThreshold) { StartIdle(); return; }

            dir.Normalize();
            _self.position += dir * moveSpeed * Time.deltaTime;

            // Face movement direction
            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir, Vector3.up);
                _self.rotation = Quaternion.Slerp(_self.rotation, look, 10f * Time.deltaTime);
            }
        }
    }

    public override void Exit()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        animator.SetBool("Walk", false);
        base.Exit();
    }

    private void StartIdle()
    {
        _isWaiting = true;
        _idleTimer = idleTime;
        if (animator != null) animator.SetBool("Walk", false);

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
    }

    private void NextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        _index = (_index + 1) % waypoints.Length;

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            var t = waypoints[_index];
            if (t != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(t.transform.position);
            }
        }

        if (animator != null) animator.SetBool("Walk", true);
    }
    public override void BuildInspectorUI(VisualElement container)
    {
        base.BuildInspectorUI(container);
        container.Add(FloatField("Move Speed", () => this.moveSpeed, v => this.moveSpeed = v));
        container.Add(FloatField("Arrive Threshold", () => this.arriveThreshold, v => this.arriveThreshold = v));
        container.Add(BoolField("Use AI to Move", () => this.useNavMeshIfAvailable, v => this.useNavMeshIfAvailable = v));
        container.Add(FloatField("Idle Time", () => this.idleTime, v => this.idleTime = v));
    }
}

public partial class Boss : MonoBehaviour
{
    private bool hasPatrolState => stateGraph != null && stateGraph.transitionNodes != null &&
                                   stateGraph.transitionNodes.Any(t => t.nextStates != null &&
                                                                       t.nextStates.Any(s => s != null && s.state is PatrolState));
    [ShowIf("hasPatrolState")] public Transform[] waypoints;                                                                   
}
