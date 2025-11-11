using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Dash Module")]
[Tooltip("Provides a short burst of movement towards a direction or target.")]
public class DashModule : EnemyModule
{
    [Header("Dash Settings")]
    public float DashDistance = 5f;
    public float DashCooldown = 3f;

    private float cooldownTimer = 0f;
    private CharacterController controller;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        controller = GetComponent<CharacterController>();
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
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
