using UnityEngine;

public class AttackState : EnemyState
{
    private float cooldownTimer;

    public AttackState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        ctx.agent.isStopped = true;
        cooldownTimer = ctx.attackCooldown;

        // ตีทันทีครั้งแรก (จะไปผูก Animation Event ก็ได้)
        ctx.ApplyDamageToPlayer();
    }

    public override void Tick()
    {
        if (!ctx.player) { fsm.SetState(ctx.Idle); return; }

        ctx.FaceTowards(ctx.player.position);

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            if (ctx.InAttackRange(ctx.player) && ctx.CanSee(ctx.player))
                fsm.SetState(ctx.AttackIdle);  // วนตีต่อ
            else
                fsm.SetState(ctx.Chase);
        }
    }

    public override void OnExit() { }
}
