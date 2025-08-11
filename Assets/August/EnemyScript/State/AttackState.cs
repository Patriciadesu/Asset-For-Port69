using UnityEngine;

public class AttackState : EnemyState
{
    private float cooldownTimer;

    public AttackState(EnemyAI ctx, StateMachine fsm) : base(ctx, fsm) { }

    public override void OnEnter()
    {
        // ถ้าไม่มีเป้าหมาย/หลุดเงื่อนไขการตี ก็ออก
        if (!ctx.player) { fsm.SetState(ctx.Idle); return; }
        if (!ctx.InAttackRange(ctx.player) || !ctx.CanSee(ctx.player))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        ctx.agent.isStopped = true;
        ctx.FaceTowards(ctx.player.position);

        // ยิงทริกเกอร์อนิเมชันโจมตี + ทำดาเมจ "หนึ่งครั้ง"
        ctx.PlayAttackAnim();
        ctx.ApplyDamageToPlayer();

        // ตั้งคูลดาวน์ก่อนจะอนุญาตให้ตีรอบถัดไป
        cooldownTimer = ctx.attackCooldown;
    }

    public override void Tick()
    {
        if (!ctx.player) { fsm.SetState(ctx.Idle); return; }

        // ถ้าหลุดระยะหรือมองไม่เห็น -> กลับไปไล่ "ทันที"
        if (!ctx.InAttackRange(ctx.player) || !ctx.CanSee(ctx.player))
        {
            fsm.SetState(ctx.Chase);
            return;
        }

        // ยังอยู่ในระยะ: หันหาศัตรูและนับคูลดาวน์เพื่อวนไป AttackIdle
        ctx.FaceTowards(ctx.player.position);

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            // พร้อมจะหน่วงก่อนตีรอบใหม่
            fsm.SetState(ctx.AttackIdle);
        }
    }

    public override void OnExit()
    {
        // ปล่อยให้ state อื่นจัดการ movement ต่อ
    }
}
