using UnityEngine;

/// <summary>
/// Abstract base class for all modular AI behaviors.
/// Inherit from this to create custom abilities and behaviors for the Killer AI.
/// </summary>
public abstract class EnemyModule : MonoBehaviour
{
    [Header("Module Settings")]
    [Tooltip("Whether this module is currently active and processing updates")]
    public bool IsActive = true;

    /// <summary>
    /// Reference to the main KillerAI controller. Set during Initialize().
    /// </summary>
    protected KillerAI killer;

    /// <summary>
    /// Called once by KillerAI during startup. Store references and perform setup here.
    /// </summary>
    /// <param name="killer">Reference to the main AI controller</param>
    public virtual void Initialize(KillerAI killer)
    {
        this.killer = killer;
    }

    /// <summary>
    /// Called immediately when the AI enters a new state.
    /// </summary>
    /// <param name="newState">The state being entered</param>
    public virtual void OnStateEnter(EnemyState newState) { }

    /// <summary>
    /// Called every frame while the module is active.
    /// </summary>
    /// <param name="currentState">The AI's current state</param>
    public virtual void OnStateUpdate(EnemyState currentState) { }

    /// <summary>
    /// Called immediately when the AI exits a state.
    /// </summary>
    /// <param name="oldState">The state being exited</param>
    public virtual void OnStateExit(EnemyState oldState) { }

    /// <summary>
    /// Helper method to damage the player using the standard TakeDamage method
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    /// <returns>True if damage was applied successfully</returns>
    protected bool DamagePlayer(float damageAmount)
    {
        Player player = Player.Instance;
        if (player != null && player.Stat != null)
        {
            player.Stat.TakeDamage(damageAmount);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the target player component from the KillerAI
    /// </summary>
    /// <returns>The Player component if available, null otherwise</returns>
    protected Player GetTargetPlayer()
    {
        return killer?.TargetPlayer;
    }
    
    /// <summary>
    /// Override this to provide custom stun duration when this module attacks.
    /// Return null to use default animation-based duration.
    /// </summary>
    /// <param name="baseAnimationDuration">The duration of the attack animation</param>
    /// <returns>Custom stun duration, or null to use default</returns>
    public virtual float? GetStunDuration(float baseAnimationDuration)
    {
        return null; // Default: use animation duration
    }
}
