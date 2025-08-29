using UnityEngine;

[System.Serializable]
public class BossIdleState : BossState
{
    public BossIdleState(Boss bossInstance) : base("Idle", bossInstance) { }
    public override void Enter()
    {
        if (animator != null) animator.SetTrigger("Idle");
    }
}
