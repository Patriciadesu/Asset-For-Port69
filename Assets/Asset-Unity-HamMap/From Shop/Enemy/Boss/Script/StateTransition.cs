using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BossStateTransition", menuName = "BossGraph/Boss State Transition")]
public class StateTransition : ScriptableObject
{
    [SerializeReference]
    public Condition condition;                    // Now a ScriptableObject derived from Condition
    public BossStateNode[] nextStates;
    public UnityEvent<BossStateNode> onConditionMet;

    private Boss boundBoss;

    public void Bind(Boss boss)
    {
        boundBoss = boss;
        if (condition != null)
            condition.Bind(boss);
    }

    public void OnConditionMet()
    {
        if (nextStates == null || nextStates.Length == 0) return;

        int randomIndex = Random.Range(0, nextStates.Length);
        BossStateNode nextState = nextStates[randomIndex];
        onConditionMet?.Invoke(nextState);
    }

    public void StartTrackCondition()
    {
        if (condition != null)
        {
            condition.onConditionMet.AddListener(OnConditionMet);

            // Bind lazily if not bound yet (fallback using scene search)
            if (boundBoss == null)
            {
                var boss = FindFirstObjectByType<Boss>();
                if (boss != null) Bind(boss);
            }

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

