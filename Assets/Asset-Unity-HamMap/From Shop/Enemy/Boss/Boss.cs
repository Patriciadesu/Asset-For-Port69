using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    public float Health = 100f;
    public BossState CurrentState;
    private void Start()
    {
        CurrentState = new BossIdleState(this);
    }
    public void Update()
    {
        switch (CurrentState.Stage)
        {
            case StateStage.Enter:
                CurrentState.Enter();
                CurrentState.Stage = StateStage.Update;
                break;
            case StateStage.Update:
                CurrentState.Update();
                break;
            case StateStage.Exit:
                CurrentState.Exit();
                // Transition to next state logic can be added here
                break;
        }
    }
    public void FixedUpdate()
    {
        if(CurrentState.Stage == StateStage.Update)
        {
            CurrentState.FixedUpdate();
        }
    }
}