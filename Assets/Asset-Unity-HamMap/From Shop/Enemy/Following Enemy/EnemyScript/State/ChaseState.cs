using UnityEngine;

public class ChaseState : EnemyState
{
    public ChaseState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        ctx.agent.isStopped = false;
        ctx.SetChasingAnim(true);
    }

    public override void Tick()
    {
        if (!ctx.player)
        {
            fsm.SetState(ctx.HasPatrol ? ctx.Patrol : ctx.Idle);
            return;
        }

        float dist = Vector3.Distance(ctx.transform.position, ctx.player.position);
        ctx.agent.SetDestination(ctx.player.position);

        if (dist <= ctx.attackRange)
        {
            fsm.SetState(ctx.AttackIdle);
            return;
        }

        if (dist > ctx.sightRange * 1.2f)
        {
            fsm.SetState(ctx.HasPatrol ? ctx.Patrol : ctx.Idle);
        }
    }

    public override void OnExit()
    {
        ctx.SetChasingAnim(false);
    }
}
