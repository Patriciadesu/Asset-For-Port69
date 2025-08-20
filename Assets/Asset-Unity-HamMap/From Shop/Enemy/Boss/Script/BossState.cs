using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;


[System.Serializable]
public abstract class BossState
{
    public string stateName;
    public StateStage stage { get; set; } = StateStage.Enter;
    protected Boss boss;
    protected Animator animator;
    protected bool isFinished =false;
    public bool IsFinished => isFinished;

    public BossState(string name, Boss bossInstance)
    {
        stateName = name;
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void BindRuntime(Boss bossInstance)
    {
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void Enter()
    {
        Debug.Log($"Entering state: {stateName}");
        stage = StateStage.Update;
    }
    public virtual void Update()      { Debug.Log($"Updating state: {stateName}"); }
    public virtual void FixedUpdate() { Debug.Log($"Fixed updating state: {stateName}"); }

    // Traditional no-arg Exit (cleanup only)
    public virtual void Exit()
    {
        Debug.Log($"Exiting state: {stateName}");
    }

    // NEW: state-driven transition API
    public virtual void Exit(BossStateNode nextState)
    {
        // Do this state's cleanup once
        Exit();

        // Ask the graph (via Boss) to switch to the requested next state
        if (boss != null && boss.stateGraph != null && nextState != null)
        {
            boss.stateGraph.ChangeState(nextState);
        }
    }
}
[System.Serializable]
public class BossIdleState : BossState
{
    public BossIdleState(Boss bossInstance) : base("Idle", bossInstance) { }
    public override void Enter()
    {
        if (animator != null) animator.SetTrigger("Idle");
    }
}

public enum StateStage { Enter, Update, Exit }

[System.Serializable]
public class BossAttackState : BossState
{
    public TimelineAsset timelinePlayable;
    public float damage;
    private PlayableDirector director;

    public BossAttackState(Boss bossInstance) : base("Attack", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        // Enable weapon colliders for this attack window
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c != null) c.enabled = true; });

        // Ensure we have a director
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();

        // Play timeline (if any) and listen for end
        if (timelinePlayable != null)
        {
            director.playableAsset = timelinePlayable;
            director.time = 0;
            director.Play();
        }
        else
        {
            // If no timeline, end after one frame by switching to Exit
            stage = StateStage.Exit;
        }
    }

    public override void Update()
    {
        base.Update();
        if (director.time >= director.duration)
        {
            OnAttackEnd();
        }
        // Attack specifics usually driven by animation/timeline events.
    }

    public override void Exit()
    {
        // Disable weapon colliders when leaving attack
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c != null) c.enabled = false; });

        if (director != null)
        {
            if (director.state == PlayState.Playing) director.Stop();
            director.playableAsset = null;
        }

        base.Exit();
    }

    private void OnAttackEnd()
    {
        isFinished = true;
        boss.onAttackEnd.Invoke();
        stage = StateStage.Exit;
    }
}
[System.Serializable]
public class BossPatrolState : BossState
{
    [Tooltip("Patrol points in the scene. The boss will loop through these.")]
    public Transform[] waypoints => boss.waypoints;

    [Tooltip("Movement speed when patrolling (used if no NavMeshAgent).")]
    public float moveSpeed = 2f;

    [Tooltip("How close to a waypoint before switching to the next.")]
    public float arriveThreshold = 0.2f;

    [Tooltip("If true and a NavMeshAgent exists on the Boss, use it for movement.")]
    public bool useNavMeshIfAvailable = true;

    private int _index;
    private Transform _self;
    private NavMeshAgent _agent;

    public BossPatrolState(Boss bossInstance) : base("Patrol", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _agent = boss != null ? boss.GetComponent<NavMeshAgent>() : null;
    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetTrigger("Walk");

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

        var target = waypoints[_index];
        if (target == null) { base.Update(); return; }

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            // Agent handles movement; check arrival
            if (!_agent.pathPending && _agent.remainingDistance <= Mathf.Max(0.05f, arriveThreshold))
            {
                NextWaypoint();
            }
        }
        else
        {
            // Transform-based movement
            Vector3 dir = (target.transform.position - _self.position);
            float dist = dir.magnitude;
            if (dist <= arriveThreshold) { NextWaypoint(); return; }

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
        base.Exit();
    }

    private void NextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        _index = (_index + 1) % waypoints.Length;

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            var t = waypoints[_index];
            if (t != null) _agent.SetDestination(t.transform.position);
        }
    }
}

[System.Serializable]
public class BossChaseState : BossState
{

    [Tooltip("Movement speed while chasing (used if no NavMeshAgent).")]
    public float moveSpeed = 3.5f;

    [Tooltip("If true and a NavMeshAgent exists on the Boss, use it for chase movement.")]
    public bool useNavMeshIfAvailable = true;

    [Tooltip("Optional direct reference to a target. If null, will search by tag once on Enter/if lost.")]
    public Transform explicitTarget => Player.Instance.transform;

    private Transform _self;
    private Transform _target;
    private NavMeshAgent _agent;

    public BossChaseState(Boss bossInstance) : base("Chase", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _agent = boss != null ? boss.GetComponent<NavMeshAgent>() : null;
    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetTrigger("Run");

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.speed = Mathf.Max(0.01f, moveSpeed);
            if (_target != null) _agent.SetDestination(_target.position);
        }
    }

    public override void Update()
    {
        if (_self == null)
        {
            base.Update();
            return;
        }

        if (_target == null)
        {
            base.Update();
            return;
        }

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            // Continuously update destination to follow a moving target
            if (!_agent.pathPending)
                _agent.SetDestination(_target.position);
        }
        else
        {
            // Simple transform chase
            Vector3 dir = (_target.position - _self.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                _self.position += dir * moveSpeed * Time.deltaTime;

                var look = Quaternion.LookRotation(dir, Vector3.up);
                _self.rotation = Quaternion.Slerp(_self.rotation, look, 10f * Time.deltaTime);
            }
        }
    }

    public override void Exit()
    {
        if (_agent != null)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        base.Exit();
    }
    
}