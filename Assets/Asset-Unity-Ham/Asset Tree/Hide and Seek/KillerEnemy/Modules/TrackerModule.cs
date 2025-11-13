using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Tracker Module")]
[Tooltip("Stores last known target position for other systems to use.")]
public class TrackerModule : EnemyModule
{
    [Header("Tracker State")]
    public Vector3 LastKnownTargetPosition;
    public bool HasLastKnownPosition;

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || killer == null) return;
        if (killer.Target != null)
        {
            LastKnownTargetPosition = killer.Target.position;
            HasLastKnownPosition = true;
        }
    }
}
