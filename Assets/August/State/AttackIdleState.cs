using UnityEngine;

public class AttackIdleState : EnemyState
{
    private float timer;

    public AttackIdleState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        timer = ctx.attackWindup;
        ctx.agent.isStopped = true;
    }

    public override void Tick()
    {
        if (!ctx.player) { fsm.SetState(ctx.Idle); return; }

        ctx.FaceTowards(ctx.player.position);

        if (!(ctx.InAttackRange(ctx.player) && ctx.CanSee(ctx.player)))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            fsm.SetState(ctx.Attack);
        }
    }

    public override void OnExit() { }
}
