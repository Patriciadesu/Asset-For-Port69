using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;

public class NextState
{
    public BossState State;
}


public class ConditionNextState : NextState
{
    public UnityEvent OnConditionMet;
    public void StartConditionTracking()
    {

    }
    public void CheckCondition()
    {
        // Logic to check if the condition is met
        // If met, invoke OnConditionMet
        OnConditionMet?.Invoke();
    }
    public void StopConditionTracking()
    {

    }

}
public class RandomNextState : NextState
{
    public float Probability;
}

public enum StateStage
{
    Enter,
    Update,
    Exit
}

public class BossState
{
    public List<ConditionNextState> ConditionNextStates;
    public List<RandomNextState> RandomNextStates;
    public StateStage Stage;
    protected Boss _boss;
    protected Animator _animator;
    public BossState(Boss boss, Animator animator)
    {
        _boss = boss;
        _animator = animator;
    }
    public BossState(Boss boss)
    {
        _boss = boss;
        _animator = _boss.GetComponent<Animator>();
        Stage = StateStage.Enter;
    }
    public virtual void Enter()
    {
        if (ConditionNextStates.Count > 0)
        {
            foreach (ConditionNextState condition in ConditionNextStates)
            {
                condition.StartConditionTracking();
                condition.OnConditionMet.AddListener(() => ChangeState(condition.State));
            }
        }
    }
    public virtual void Exit()
    {
        if (ConditionNextStates.Count > 0)
        {
            foreach (ConditionNextState condition in ConditionNextStates)
            {
                condition.StopConditionTracking();
                condition.OnConditionMet.RemoveAllListeners();
            }
        }
        _boss.CurrentState = null;
    }
    public virtual void Update()
    {
        // Logic for updating the state
    }
    public virtual void FixedUpdate()
    {
        // Logic for fixed updates
    }
    public void ChangeState(BossState newState)
    {
        Exit();
        _boss.CurrentState = newState;
        newState.Enter();
    }
    public void RandomState()
    {
        if (RandomNextStates.Count > 0)
        {
            var randomState = RandomNextStates
                .Where(s => UnityEngine.Random.value < s.Probability)
                .OrderBy(s => UnityEngine.Random.value)
                .FirstOrDefault();

            if (randomState != null)
            {
                ChangeState(randomState.State);
            }
        }
    }
}

public class BossIdleState : BossState
{
    public float IdleTime;
    private float _idleTimer;

    public BossIdleState(Boss boss) : base(boss) { }

    public override void Enter()
    {
        base.Enter();
        _idleTimer = 0f;
        _animator.SetTrigger("Idle");
    }
    public override void Update()
    {
        if (_idleTimer >= IdleTime)
        {
            Stage = StateStage.Exit;
        }
    }
    public override void FixedUpdate()
    {
        _idleTimer += Time.deltaTime;
    }
    public override void Exit()
    {
        base.Exit();
        _idleTimer = 0f;
    }
}