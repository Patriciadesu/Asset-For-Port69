using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Killer AI/Modules/Patrol Module")]
[Tooltip("Implements patrol movement between assigned points. Handles starting/stopping patrol independent of KillerAI.")]
public class PatrolModule : EnemyModule
{
    [Header("Patrol Points")]
    public Transform[] Points;
    public bool AutoStartFromIdle = true;
    public bool Loop = true;
    public float ArriveDistance = 1f;
    public float RotationLerp = 5f;

    [Header("Speed")]
    public bool UseModuleSpeed = false;
    public float ModulePatrolSpeed = 3f;

    private int currentIndex = 0;
    private CharacterController controller;
    private NavMeshAgent agent;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null) return;

        // Optionally auto-start patrol when idle and points are configured
        if (AutoStartFromIdle && currentState == EnemyState.Idle && HasValidPoints())
        {
            killer.ChangeState(EnemyState.Patrol);
            return;
        }

        if (currentState != EnemyState.Patrol)
            return;

        if (!HasValidPoints())
        {
            // If no points, leave patrol to idle
            killer.ChangeState(EnemyState.Idle);
            return;
        }

        Transform target = Points[currentIndex];
        if (target == null)
        {
            AdvanceIndex();
            return;
        }

        float speed = UseModuleSpeed ? ModulePatrolSpeed : killer.PatrolSpeed;

        // Use NavMeshAgent if available
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(target.position);
            agent.stoppingDistance = ArriveDistance;

            // Check if we arrived at the patrol point
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (Loop)
                {
                    AdvanceIndex();
                }
                else if (currentIndex < Points.Length - 1)
                {
                    currentIndex++;
                }
                else
                {
                    // End of path
                    killer.ChangeState(EnemyState.Idle);
                }
            }
        }
        else
        {
            // Fallback to manual movement if NavMeshAgent not available
            Vector3 to = (target.position - transform.position);
            to.y = 0f;
            Vector3 dir = to.normalized;

            if (controller != null)
            {
                controller.Move(dir * speed * Time.deltaTime);
            }
            else
            {
                transform.position += dir * speed * Time.deltaTime;
            }

            // Rotate towards movement direction
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * RotationLerp
                );
            }

            // Arrival check
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= ArriveDistance)
            {
                if (Loop)
                {
                    AdvanceIndex();
                }
                else if (currentIndex < Points.Length - 1)
                {
                    currentIndex++;
                }
                else
                {
                    // End of path
                    killer.ChangeState(EnemyState.Idle);
                }
            }
        }
    }

    private bool HasValidPoints()
    {
        return Points != null && Points.Length > 0;
    }

    private void AdvanceIndex()
    {
        if (!HasValidPoints()) return;
        currentIndex = (currentIndex + 1) % Points.Length;
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasValidPoints()) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < Points.Length; i++)
        {
            var p = Points[i];
            if (p == null) continue;
            Gizmos.DrawWireSphere(p.position, 0.5f);
            int next = (i + 1) % Points.Length;
            if (Loop && Points[next] != null)
            {
                Gizmos.DrawLine(p.position, Points[next].position);
            }
        }
    }
}
