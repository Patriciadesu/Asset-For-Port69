using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BossStateNode", menuName = "BossGraph/Boss State Node")]
public class BossStateNode : ScriptableObject
{
    public string stateName;
    public BossState state;
    public bool isInitialState;

    public UnityEvent<BossStateNode> onStateChange;
    public StateTransition[] transitions;

    public void OnConditionMet(BossStateNode nextState)
    {
        onStateChange.Invoke(nextState);
        StopTrackingConditions();
    }

    public void StartTrackingConditions()
    {
        foreach (StateTransition transition in transitions)
        {
            if (transition.condition != null)
            {
                transition.StartTrackCondition();
                transition.onConditionMet.AddListener(OnConditionMet);
            }
        }
    }
    public void StopTrackingConditions()
    {
        foreach (var transition in transitions)
        {
            if (transition.condition != null)
            {
                transition.StopTrackCondition();
                transition.onConditionMet.RemoveListener(OnConditionMet);
            }
        }
    }

}