using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BossStateNode", menuName = "BossGraph/Boss State Node")]
public class BossStateNode : ScriptableObject
{
    public string stateName;
    public bool isInitialState;

    [SerializeReference] public BossState state;

    public StateTransition[] transitions;
    public UnityEvent<BossStateNode> onStateChange = new UnityEvent<BossStateNode>();
    [HideInInspector]public Vector2 position;
    private Boss _cachedBoss;

    public void StartTrackingConditions()
    {
        if (transitions == null) return;

        if (_cachedBoss == null)
            _cachedBoss = Object.FindFirstObjectByType<Boss>();

        for (int i = 0; i < transitions.Length; i++)
        {
            var t = transitions[i];
            if (t == null) continue;

            t.Bind(_cachedBoss);
            t.onConditionMet.RemoveListener(onStateChange.Invoke);
            t.onConditionMet.AddListener(onStateChange.Invoke);
            t.StartTrackCondition();
        }

        // Bind runtime references for state logic
        if (state != null && _cachedBoss != null)
            state.BindRuntime(_cachedBoss);
    }

    public void StopTrackingConditions()
    {
        if (transitions == null) return;

        for (int i = 0; i < transitions.Length; i++)
        {
            var t = transitions[i];
            if (t == null) continue;

            t.onConditionMet.RemoveListener(onStateChange.Invoke);
            t.StopTrackCondition();
        }
    }
}
