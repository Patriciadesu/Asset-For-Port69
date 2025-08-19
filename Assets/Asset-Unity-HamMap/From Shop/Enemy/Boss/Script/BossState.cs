using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;


[System.Serializable]
public abstract class BossState
{
    public string stateName;
    public StateStage stage { get; set; } = StateStage.Enter;
    protected Boss boss;
    protected Animator animator;

    public BossState(string name, Boss bossInstance)
    {
        stateName = name;
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void BindRuntime(Boss bossInstance)
    {
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void Enter()
    {
        Debug.Log($"Entering state: {stateName}");
        stage = StateStage.Update;
    }
    public virtual void Update()      {Debug.Log($"Updating state: {stateName}");}
    public virtual void FixedUpdate() {Debug.Log($"Fixed updating state: {stateName}");}
    public virtual void Exit()        {Debug.Log($"Exiting state: {stateName}");}
}

[System.Serializable]
public class BossIdleState : BossState
{
    public BossIdleState(Boss bossInstance) : base("Idle", bossInstance) { }
    public override void Enter()
    {
        if (animator != null) animator.SetTrigger("Idle");
    }
}

public enum StateStage { Enter, Update, Exit }

[System.Serializable]
public class BossAttackState : BossState
{
    public TimelineAsset timelinePlayable;
    public float damage;
    public BossAttackState(Boss bossInstance) : base("Attack", bossInstance) { }

    public override void Enter()
    {
        base.Enter();
        // Additional logic for entering attack state
        //animator.SetTrigger("Attack");
    }

    public override void Update()
    {
        base.Update();
        Debug.Log("Boss is attacking in state: " + stateName);
        // Logic for attack behavior
    }
}
