using UnityEngine;

public class PatrolState : EnemyState
{
    public PatrolState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        ctx.agent.isStopped = false;
        if (ctx.HasPatrol) ctx.agent.SetDestination(ctx.CurrentPatrolPoint);
    }

    public override void Tick()
    {
        if (ctx.player && ctx.CanSee(ctx.player))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        if (!ctx.HasPatrol) { fsm.SetState(ctx.Idle); return; }

        if (!ctx.agent.pathPending)
            ctx.agent.SetDestination(ctx.CurrentPatrolPoint);

        if (ctx.Reached(ctx.CurrentPatrolPoint))
        {
            ctx.AdvancePatrol();
            fsm.SetState(ctx.Idle); // แวะพักสั้น ๆ ให้ดูมีชีวิต
        }
    }

    public override void OnExit() { }
}
