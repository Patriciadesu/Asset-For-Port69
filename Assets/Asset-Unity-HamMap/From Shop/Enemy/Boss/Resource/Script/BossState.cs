using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;

public enum StateStage { Enter, Update, Exit }

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

//-------------------------//
//  Boss Movement States  //
//-------------------------//
#region Boss Movement States
[System.Serializable]
public class BossIdleState : BossState
{
    public BossIdleState(Boss bossInstance) : base("Idle", bossInstance) { }
    public override void Enter()
    {
        if (animator != null) animator.SetTrigger("Idle");
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
        if (animator != null) animator.SetBool("Walk",true);

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
        animator.SetBool("Walk", false);
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
    private Transform _target=> Player.Instance.transform;
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
        if (animator != null) animator.SetBool("Walk",true);

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
        if (animator != null) animator.SetBool("Walk",false);
        base.Exit();
    }

}
#endregion


//-------------------------//
//    Boss Combat States   //
//-------------------------//
[System.Serializable]
public class BossAttackState : BossState
{
    public TimelineAsset timelinePlayable;
    public float damage;

    private PlayableDirector director;
    private bool subscribed;
    private bool endedOnce;

    // Safety fallback if timeline has 0 duration or director misbehaves
    private const double MinDurationEpsilon = 1e-3;
    private float safetyTimer;
    private const float SafetyTimeout = 0.25f; // seconds, end quickly when no valid timeline

    public BossAttackState(Boss bossInstance) : base("Attack", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        endedOnce = false;
        safetyTimer = 0f;

        // 1) Toggle attack colliders ON
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c) c.enabled = true; });

        // 2) Ensure we have a director
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();

        // 3) Prepare & play timeline (if any)
        if (timelinePlayable != null)
        {
            // Always rebuild to avoid stale graphs on re-entry
            director.time = 0;
            director.extrapolationMode = DirectorWrapMode.None; // end cleanly, don’t loop
            director.playOnAwake = false;
            director.playableAsset = null; // clear old asset to avoid stale state
            director.playableAsset = timelinePlayable;

            // Unsubscribe old (defensive) then subscribe
            if (subscribed)
            {
                director.stopped -= OnDirectorStopped;
                subscribed = false;
            }
            director.stopped += OnDirectorStopped;
            subscribed = true;

            // Rebuild graph to get correct duration immediately
            director.RebuildGraph();
            director.Evaluate(); // evaluate bindings and initial state
            director.playableGraph.GetRootPlayable(0).SetSpeed(boss.attackAnimationSpeedMultiplier);
            director.Play();

            // If somehow duration is invalid/zero, we’ll end via safety timer below
        }
        else
        {
            // No timeline → exit shortly (single frame might be too abrupt in some loops)
            stage = StateStage.Update; // allow one update
            safetyTimer = 0f;
        }
    }

    public override void Update()
    {
        base.Update();

        // If we have a valid director + graph, check for completion ourselves too
        if (director != null && director.playableGraph.IsValid())
        {
            // Handle zero/near-zero duration timelines via safety timer
            var duration = director.duration;
            if (duration <= MinDurationEpsilon)
            {
                safetyTimer += Time.deltaTime;
                if (safetyTimer >= SafetyTimeout)
                {
                    OnAttackEnd();
                    return;
                }
            }
            else
            {
                // Normal case: if time >= duration, end (covers cases where stopped event is missed)
                if (director.state != PlayState.Playing || director.time + MinDurationEpsilon >= duration)
                {
                    OnAttackEnd();
                    return;
                }
            }
        }
        else
        {
            // No director / invalid graph / no timeline → end via safety timer
            safetyTimer += Time.deltaTime;
            if (safetyTimer >= SafetyTimeout)
            {
                OnAttackEnd();
                return;
            }
        }
    }

    public override void Exit()
    {
        // Toggle attack colliders OFF (idempotent)
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c) c.enabled = false; });

        // Stop & clean director safely
        if (director != null)
        {
            if (director.state == PlayState.Playing) director.Stop();

            if (subscribed)
            {
                director.stopped -= OnDirectorStopped;
                subscribed = false;
            }

            // Clear playable asset so re-entry can set fresh graph
            director.playableAsset = null;
        }

        base.Exit();
    }

    private void OnDirectorStopped(PlayableDirector _)
    {
        // Unity can invoke .stopped multiple times in some edge cases—guard this.
        OnAttackEnd();
    }

    private void OnAttackEnd()
    {
        if (endedOnce) return; // ensure single exit path
        endedOnce = true;

        isFinished = true;
        // If boss has a UnityEvent, guard null & no listeners scenarios
        if (boss.onAttackEnd != null)
            boss.onAttackEnd.Invoke();

        stage = StateStage.Exit;
    }
}
[System.Serializable]
public class BossTeleportState : BossState
{
    public enum TeleportPosition
    {
        OnTopOfPlayer,
        RandomAroundPlayer,
        BehindPlayer,
        RandomAroundBoss,
        BackToInitialPosition
    }
    public TeleportPosition teleportPosition = TeleportPosition.OnTopOfPlayer;

    public BossTeleportState(string name, Boss bossInstance) : base("Teleport", bossInstance)
    {

    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetTrigger("Teleport");

        Vector3 targetPosition = Vector3.zero;
        switch (teleportPosition)
        {
            case TeleportPosition.OnTopOfPlayer:
                targetPosition = Player.Instance.transform.position + Vector3.up * boss.transform.position.y; // Teleport directly above player
                break;
            case TeleportPosition.RandomAroundPlayer:
                targetPosition = Player.Instance.transform.position + UnityEngine.Random.insideUnitSphere * 5f;
                targetPosition.y = 0; // Keep on ground
                break;
            case TeleportPosition.BehindPlayer:
                var playerDir = (Player.Instance.transform.position - boss.transform.position).normalized;
                targetPosition = Player.Instance.transform.position - playerDir * 3f + Vector3.up * 2f;
                break;
            case TeleportPosition.RandomAroundBoss:
                targetPosition = boss.transform.position + UnityEngine.Random.insideUnitSphere * 5f;
                targetPosition.y = 0; // Keep on ground
                break;
            case TeleportPosition.BackToInitialPosition:
                boss.ResetTransform();
                targetPosition = boss.initialPosition;
                break;
        }

        boss.transform.position = targetPosition;
        isFinished = true; // Mark as finished immediately
        boss.onAttackEnd.Invoke(); // Trigger any attack end logic immediately after teleport
    }

}
[System.Serializable]
public class BossPhaseChangeState : BossState
{
    public float newMaxHealth = 1f;
    public float newAnimationSpeedMultiplier = 1f;
    public float newSpeed = 2f;
    public TimelineAsset changePhaseTimeline;

    private PlayableDirector director;
    private bool subscribed;

    public BossPhaseChangeState(Boss bossInstance) : base("Phase Change", bossInstance) { }

    public override void Enter()
    {
        base.Enter();
        // Ensure we have a director
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();

        // Prepare & play timeline (if any)
        if (changePhaseTimeline != null)
        {
            director.time = 0;
            director.extrapolationMode = DirectorWrapMode.None; // end cleanly, don’t loop
            director.playOnAwake = false;
            director.playableAsset = null; // clear old asset to avoid stale state
            director.playableAsset = changePhaseTimeline;

            // Unsubscribe old (defensive) then subscribe
            if (subscribed)
            {
                director.stopped -= OnDirectorStopped;
                subscribed = false;
            }
            director.stopped += OnDirectorStopped;
            subscribed = true;

            // Rebuild graph to get correct duration immediately
            director.RebuildGraph();
            director.Evaluate(); // evaluate bindings and initial state
            director.Play();
        }
        else
        {
            // No timeline → exit shortly (single frame might be too abrupt in some loops)
            stage = StateStage.Update; // allow one update
        }
    }

    public override void Update()
    {
        base.Update();

        // If we have a valid director + graph, check for completion ourselves too
        if (director != null && director.playableGraph.IsValid())
        {
            if (director.state != PlayState.Playing ||
                director.time >= director.duration - 1e-3) // near-end epsilon
            {
                OnPhaseChangeEnd();
                return;
            }
        }
    }

    public override void Exit()
    {
        if (director != null)
        {
            if (director.state == PlayState.Playing) director.Stop();

            if (subscribed)
            {
                subscribed = false;
            }
        }
    }
    private void OnDirectorStopped(PlayableDirector _)
    {
        OnPhaseChangeEnd();
    }
    private void OnPhaseChangeEnd()
    {
        boss.health = newMaxHealth;
        boss.attackAnimationSpeedMultiplier = newAnimationSpeedMultiplier;
        boss.speed = newSpeed;
        
        isFinished = true;
        // If boss has a UnityEvent, guard null & no listeners scenarios
        if (boss.onAttackEnd != null)
            boss.onAttackEnd.Invoke();

        stage = StateStage.Exit;
    }
}