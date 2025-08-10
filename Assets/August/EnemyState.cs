using UnityEngine;

public abstract class EnemyState : IState
{
    protected readonly EnemyAI ctx;
    protected readonly StateMachine fsm;

    protected EnemyState(EnemyAI context, StateMachine stateMachine)
    {
        ctx = context;
        fsm = stateMachine;
    }

    public abstract void OnEnter();
    public abstract void Tick();
    public abstract void OnExit();
}
