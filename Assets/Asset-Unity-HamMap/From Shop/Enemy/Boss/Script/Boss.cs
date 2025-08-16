using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    public BossStateGraph stateGraph;
    [HideInInspector]public UnityEvent onStateChanged;
    [HideInInspector]public UnityEvent<float> onStateTimeChanged;
    [HideInInspector]public UnityEvent<float> onHealthChanged;
    [HideInInspector]public UnityEvent onPlayerInSight;
    [HideInInspector]public UnityEvent onPlayerOutOfSight;
    [HideInInspector]public UnityEvent onPlayerInAttackRange;
    [HideInInspector]public UnityEvent onPlayerOutOfAttackRange;

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