using UnityEngine;

/// <summary>
/// Optional module that bridges the KillerAI state machine to the DemonAnimationController.
/// Safe to remove – KillerAI will still function and DemonAnimationController can poll state on its own.
/// </summary>
public class DemonAnimationModule : EnemyModule
{
    [Header("Animation")]
    [Tooltip("Reference to the DemonAnimationController (auto-assigned if left empty)")]
    [SerializeField] private DemonAnimationController animationController;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);

        if (animationController == null)
        {
            animationController = GetComponent<DemonAnimationController>();
            if (animationController == null)
                animationController = GetComponentInChildren<DemonAnimationController>();
        }

        if (animationController == null)
        {
            Debug.LogWarning("[DemonAnimationModule] No DemonAnimationController found – module will be inert.");
        }
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive || animationController == null) return;
        animationController.OnModuleStateEnter(newState);
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || animationController == null) return;
        animationController.OnModuleStateUpdate(currentState);
    }

    public override void OnStateExit(EnemyState oldState)
    {
        if (!IsActive || animationController == null) return;
        animationController.OnModuleStateExit(oldState);
    }
}
