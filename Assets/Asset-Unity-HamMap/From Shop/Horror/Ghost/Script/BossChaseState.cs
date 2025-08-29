using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;

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

