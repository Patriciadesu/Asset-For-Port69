using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Noise Detection Module")]
[Tooltip("Allows external systems to report noises for the AI to react to.")]
public class NoiseDetectionModule : EnemyModule
{
    [Header("Hearing Settings")]
    public float HearingRadius = 15f;

    [Header("Runtime State")] 
    public Vector3 LastHeardPosition;
    public bool HasNoise;

    public void ReportNoise(Vector3 position, float loudness = 1f)
    {
        if (!IsActive || killer == null) return;
        float radius = HearingRadius * Mathf.Clamp(loudness, 0.1f, 10f);
        float d = Vector3.Distance(transform.position, position);
        if (d <= radius)
        {
            LastHeardPosition = position;
            HasNoise = true;
            if (killer.CurrentState == EnemyState.Patrol || killer.CurrentState == EnemyState.Idle)
            {
                killer.ChangeState(EnemyState.Chase);
            }
        }
    }
}
