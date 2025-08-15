using UnityEngine;
using System.Collections.Generic;

public class Boss : MonoBehaviour
{
    public BossStateGraph stateGraph;

    private void Start()
    {
        if (stateGraph != null)
        {
            stateGraph.Awake();
            stateGraph.StartState();
        }
    }
    
    private void Update()
    {
        if (stateGraph != null && stateGraph.currentState != null)
        {
            switch (stateGraph.currentState.state.stage)
            {
                case StateStage.Enter:
                    stateGraph.currentState.state.Enter();
                    break;
                case StateStage.Update:
                    stateGraph.currentState.state.Update();
                    break;
                case StateStage.Exit:
                    stateGraph.currentState.state.Exit();
                    break;
            }
        }
    }
    private void FixedUpdate()
    {
        if (stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state.stage == StateStage.Update)
        {
            stateGraph.currentState.state.FixedUpdate();
        }
    }
    // Add methods to handle boss behavior based on the state graph
}