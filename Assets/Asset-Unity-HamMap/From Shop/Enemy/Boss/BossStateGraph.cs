using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "BossStateGraph", menuName = "BossGraph/Boss State Graph")]
public class BossStateGraph : ScriptableObject
{
    public BossStateNode[] stateNodes;
    public BossStateNode currentState;

    public void Awake()
    {
        currentState = stateNodes.Any(node => node.isInitialState) ? stateNodes.First(node => node.isInitialState) : null;
    }
    public void StartState()
    {
        if (currentState != null)
        {
            currentState.state.stage = StateStage.Enter;
            currentState.StartTrackingConditions();
            currentState.onStateChange.AddListener(ChangeState);
        }
    }
    public void ChangeState(BossStateNode nextState)
    {
        if (currentState != null)
        {
            currentState.state.stage = StateStage.Exit;
        }

        currentState = nextState;
        if (currentState != null)
        {
            StartState();
        }
    }

}