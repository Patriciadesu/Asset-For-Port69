using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Dash Module")]
[Tooltip("Provides a short burst of movement towards a direction or target.")]
public class DashModule : EnemyModule
{
    [Header("Dash Settings")]
    public float DashDistance = 5f;
    public float DashCooldown = 3f;
    [Tooltip("Allow the module to automatically dash during chase using random rolls.")]
    public bool EnableChaseDash = true;

    [Header("Chance Settings")]
    [SerializeField] private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.3f,
        Interval = new Vector2(2f, 4f),
        InitialDelay = new Vector2(0.5f, 1.2f)
    };

    private float cooldownTimer = 0f;
    private CharacterController controller;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        controller = GetComponent<CharacterController>();
        chaseTrigger?.Prime();
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!IsActive || !EnableChaseDash || killer == null)
            return;

        if (currentState != EnemyState.Chase)
            return;

        if (cooldownTimer > 0f)
            return;

        chaseTrigger?.PrimeIfNeeded();
        if (chaseTrigger != null && chaseTrigger.TryConsumeTrigger())
        {
            if (TryDashTowardsTarget())
            {
                chaseTrigger.BlockFor(DashCooldown);
            }
            else
            {
                chaseTrigger.BlockFor(1f);
            }
        }
    }

    public bool TryDashTowardsTarget()
    {
        if (!IsActive || killer == null || controller == null || killer.Target == null)
            return false;
        if (cooldownTimer > 0f)
            return false;

        Vector3 dir = (killer.Target.position - transform.position).normalized;
        dir.y = 0f;
        controller.Move(dir * DashDistance);
        cooldownTimer = DashCooldown;
        return true;
    }
}
