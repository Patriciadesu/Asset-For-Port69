using UnityEngine;
using System.Collections.Generic;

public class BossState
{
    public string stateName;
    public StateStage stage;
    private Boss boss;
    private Animator animator;

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

public enum StateStage
{
    Enter,
    Update,
    Exit
}