using UnityEngine;
using System.Collections.Generic;

public abstract class BossState
{
    public string stateName;
    public StateStage stage;
    protected Boss boss;
    protected Animator animator;

    public BossState(string name, Boss bossInstance)
    {
        stateName = name;
        boss = bossInstance;
        animator = boss.GetComponent<Animator>();
    }

    public virtual void Enter()
    {
        // Logic for entering the state
        Debug.Log("Entering state: " + stateName);
    }

    public virtual void Update()
    {
        // Logic for updating the state
        Debug.Log("Updating state: " + stateName);
    }

    public virtual void FixedUpdate()
    {
        // Logic for fixed updates in the state
        Debug.Log("Fixed updating state: " + stateName);
    }

    public virtual void Exit()
    {
        // Logic for exiting the state
        Debug.Log("Exiting state: " + stateName);
    }
}

public class BossIdleState : BossState
{
    public BossIdleState(Boss bossInstance) : base("Idle", bossInstance) { }

    public override void Enter()
    {
        base.Enter();
        // Additional logic for entering idle state
        animator.SetTrigger("Idle");
    }

    public override void Update()
    {
        base.Update();
    }
}

public enum StateStage
{
    Enter,
    Update,
    Exit
}