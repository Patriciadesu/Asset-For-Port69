using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BossStateTransition", menuName = "BossGraph/Boss State Transition")]
public class StateTransition : ScriptableObject
{
    public Condition condition;
    public BossStateNode[] nextStates;
    public UnityEvent<BossStateNode> onConditionMet;
    public void OnConditionMet()
    {
        int randomIndex = Random.Range(0, nextStates.Length);
        BossStateNode nextState = nextStates[randomIndex];
        onConditionMet.Invoke(nextState);
    }
    public void StartTrackCondition()
    {
        if (condition != null)
        {
            condition.onConditionMet.AddListener(OnConditionMet);
            condition.StartTrackCondition();
        }
    }
    public void StopTrackCondition()
    {
        if (condition != null)
        {
            condition.onConditionMet.RemoveListener(OnConditionMet);
            condition.StopTrackCondition();
        }
    }
   
}

public class Condition
{
    public UnityEvent onConditionMet;

    public void StartTrackCondition()
    {
        // Logic to start tracking the condition
        Debug.Log("Started tracking condition: " + this);
    }
    public void CheckCondition()
    {
        onConditionMet.Invoke();
    }
    public void StopTrackCondition()
    {
        // Logic to stop tracking the condition
        Debug.Log("Stopped tracking condition: " + this);
    }

}