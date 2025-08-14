using UnityEngine;

public class PatrolState : EnemyState
{
    private Vector3 currentTarget;

    public PatrolState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        if (!ctx.HasPatrol) { fsm.SetState(ctx.Idle); return; }
        ctx.agent.isStopped = false;
        PickNewTarget();
    }

    public override void Tick()
    {
        if (ctx.player && ctx.CanSee(ctx.player))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        if (!ctx.HasPatrol) { fsm.SetState(ctx.Idle); return; }

        if (ctx.Reached(currentTarget) || (!ctx.agent.pathPending && ctx.agent.remainingDistance <= ctx.agent.stoppingDistance))
        {
            fsm.SetState(ctx.Idle);
            return;
        }

        if (!ctx.agent.hasPath || ctx.agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid || ctx.agent.isPathStale)
        {
            PickNewTarget();
        }
    }

    public override void OnExit() { }

    private void PickNewTarget()
    {
        currentTarget = ctx.GetRoamPoint();
        ctx.agent.SetDestination(currentTarget);
    }
}
