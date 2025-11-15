using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class KillerAI : MonoBehaviour
{
    [Header("Locker Investigation")]
    [Tooltip("Radius used when placing investigation points around a locker.")]
    [SerializeField] private float lockerCheckRadius = 11f;

    [Tooltip("How close the agent must get to consider a point reached.")]
    [SerializeField] private float lockerPointTolerance = 0.5f;

    [Tooltip("Time to wait at each locker point before moving to the next.")]
    [SerializeField] private float lockerPauseDuration = 1.5f;

    private readonly List<Vector3> lockerPatrolPoints = new List<Vector3>();
    private int lockerPatrolIndex = 0;
    private bool lockerWaiting = false;
    private float lockerResumeTime = 0f;
    private Transform lockerUnderInvestigation;

    partial void LockerIdleOverride(ref bool handled)
    {
        if (TryGetLockerTransform(out var locker))
        {
            StartLockerInvestigation(locker);
            handled = true;
        }
    }

    partial void LockerPatrolOverride(ref bool handled)
    {
        if (TryGetLockerTransform(out var locker))
        {
            StartLockerInvestigation(locker);
            handled = true;
        }
    }

    partial void LockerChaseOverride(ref bool handled)
    {
        if (TryGetLockerTransform(out var locker))
        {
            StartLockerInvestigation(locker);
            handled = true;
            return;
        }

        if (IsPlayerHidden())
        {
            ChangeState(EnemyState.Patrol);
            handled = true;
        }
    }

    partial void LockerAttackOverride(ref bool handled)
    {
        if (TryGetLockerTransform(out var locker))
        {
            StartLockerInvestigation(locker);
            handled = true;
            return;
        }

        if (IsPlayerHidden())
        {
            ChangeState(EnemyState.Patrol);
            handled = true;
        }
    }

    partial void LockerCheckOverride(ref bool handled)
    {
        if (CurrentState != EnemyState.Check)
            return;

        handled = true;

        if (lockerUnderInvestigation == null)
        {
            ExitLockerInvestigation();
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (!IsPlayerHidden())
        {
            ExitLockerInvestigation();
            ChangeState(EnemyState.Chase);
            return;
        }

        if (agent == null)
            return;

        if (lockerPatrolPoints.Count == 0)
        {
            BuildLockerCheckPoints(lockerUnderInvestigation);
            if (lockerPatrolPoints.Count == 0)
            {
                ExitLockerInvestigation();
                ChangeState(EnemyState.Patrol);
                return;
            }
        }

        if (lockerWaiting)
        {
            if (Time.time >= lockerResumeTime)
            {
                lockerWaiting = false;
                AdvanceLockerPoint();
            }
            return;
        }

        Vector3 currentDestination = lockerPatrolPoints[lockerPatrolIndex];
        float distance = Vector3.Distance(transform.position, currentDestination);

        if (distance <= lockerPointTolerance)
        {
            lockerWaiting = true;
            lockerResumeTime = Time.time + lockerPauseDuration;
            agent.isStopped = true;
            return;
        }

        if (!agent.hasPath || Vector3.Distance(agent.destination, currentDestination) > 0.25f)
        {
            MoveToLockerPoint(lockerPatrolIndex);
        }
    }

    private static System.Type lockerStateType;
    private static System.Reflection.PropertyInfo lockerStateIsHidingProp;
    private static System.Reflection.PropertyInfo lockerStateCurrentProp;

    private bool TryGetLockerTransform(out Transform locker)
    {
        locker = null;

        if (!EnsureLockerStateReflection()) return false;

        bool hidden = (bool)lockerStateIsHidingProp.GetValue(null);
        if (!hidden) return false;

        var current = lockerStateCurrentProp.GetValue(null) as Transform;
        if (current)
        {
            locker = current;
            return true;
        }

        return false;
    }

    private bool IsPlayerHidden()
    {
        if (!EnsureLockerStateReflection()) return false;

        if (!(bool)lockerStateIsHidingProp.GetValue(null))
            return false;

        return lockerStateCurrentProp.GetValue(null) is Transform current && current;
    }

    private static bool EnsureLockerStateReflection()
    {
        if (lockerStateIsHidingProp != null && lockerStateCurrentProp != null)
            return true;

        if (lockerStateType == null)
        {
            lockerStateType = System.Type.GetType("LockerState") ??
                              System.Type.GetType("LockerState, Assembly-CSharp");
        }

        if (lockerStateType == null) return false;

        lockerStateIsHidingProp = lockerStateType.GetProperty("IsHiding", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        lockerStateCurrentProp = lockerStateType.GetProperty("CurrentLocker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        return lockerStateIsHidingProp != null && lockerStateCurrentProp != null;
    }

    private void StartLockerInvestigation(Transform locker)
    {
        if (locker == null || agent == null)
            return;

        lockerUnderInvestigation = locker;
        BuildLockerCheckPoints(lockerUnderInvestigation);

        if (lockerPatrolPoints.Count == 0)
        {
            lockerUnderInvestigation = null;
            return;
        }

        lockerPatrolIndex = 0;
        lockerWaiting = false;
        lockerResumeTime = 0f;
        MoveToLockerPoint(lockerPatrolIndex);

        if (CurrentState != EnemyState.Check)
        {
            ChangeState(EnemyState.Check);
        }
    }

    private void ExitLockerInvestigation()
    {
        lockerPatrolPoints.Clear();
        lockerUnderInvestigation = null;
        lockerWaiting = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = stopDistance;
        }
    }

    private void BuildLockerCheckPoints(Transform locker)
    {
        lockerPatrolPoints.Clear();
        if (locker == null)
            return;

        Vector3 center = locker.position;
        Vector3 forward = locker.forward;
        Vector3 right = locker.right;

        Vector3[] directions =
        {
            forward,
            -forward,
            right,
            -right
        };

        foreach (var dir in directions)
        {
            if (dir == Vector3.zero)
                continue;

            Vector3 candidate = center + dir.normalized * lockerCheckRadius;
            candidate.y = center.y;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                lockerPatrolPoints.Add(hit.position);
            }
            else
            {
                lockerPatrolPoints.Add(candidate);
            }
        }
    }

    private void MoveToLockerPoint(int index)
    {
        if (agent == null || lockerPatrolPoints.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, lockerPatrolPoints.Count - 1);
        Vector3 destination = lockerPatrolPoints[index];

        lockerWaiting = false;
        agent.isStopped = false;
        agent.speed = PatrolSpeed;
        agent.stoppingDistance = Mathf.Max(0.05f, lockerPointTolerance * 0.5f);
        agent.SetDestination(destination);
    }

    private void AdvanceLockerPoint()
    {
        if (lockerPatrolPoints.Count == 0)
            return;

        lockerPatrolIndex = (lockerPatrolIndex + 1) % lockerPatrolPoints.Count;
        MoveToLockerPoint(lockerPatrolIndex);
    }

}

