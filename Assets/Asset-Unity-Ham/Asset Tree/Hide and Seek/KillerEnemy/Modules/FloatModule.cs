using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Float Module")]
[Tooltip("Optional hovering/bobbing effect. Purely visual placeholder.")]
public class FloatModule : EnemyModule
{
    [Header("Float Settings")]
    public bool EnableBobbing = false;
    public float Amplitude = 0.25f;
    public float Frequency = 1.5f;

    private float baseY;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        baseY = transform.position.y;
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || !EnableBobbing) return;
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * Frequency) * Amplitude;
        transform.position = p;
    }
}
