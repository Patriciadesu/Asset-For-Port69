using UnityEngine;

public class EnemyIdleState : EnemyState
{
    private float timer;

    public EnemyIdleState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        ctx.agent.isStopped = true;
        timer = ctx.idleTime;
    }

    public override void Tick()
    {
        if (ctx.player && ctx.CanSee(ctx.player))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (ctx.HasPatrol) fsm.SetState(ctx.Patrol);
            else timer = ctx.idleTime; // ไม่มี patrol ก็ยืนนิ่งต่อ
        }
    }

    public override void OnExit() { }
}
