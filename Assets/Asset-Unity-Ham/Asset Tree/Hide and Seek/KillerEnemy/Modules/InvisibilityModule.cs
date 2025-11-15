using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Invisibility Module")]
[Tooltip("Toggles renderers on state changes for simple invisibility.")]
public class InvisibilityModule : EnemyModule
{
    [Header("Invisibility Settings")]
    public bool HideInPatrol = false;
    public bool HideInChase = false;
    public bool HideInAttack = false;
    [Tooltip("Use cooldown-based invisibility pulses instead of instant state toggles.")]
    public bool UseCooldown = true;
    [Tooltip("How long the enemy stays invisible after triggering (seconds).")]
    public float InvisibleDuration = 2.5f;
    [Tooltip("Cooldown between invisibility pulses (seconds).")]
    public float CooldownDuration = 6f;

    private Renderer[] renderers;
    private bool isInvisible;
    private float invisibilityEndTime;
    private float nextAvailableTime;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        renderers = GetComponentsInChildren<Renderer>(true);
        nextAvailableTime = Time.time + Mathf.Max(0.5f, CooldownDuration);
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (!UseCooldown)
        {
            SetVisible(!ShouldHide(newState));
        }
        else if (!ShouldHide(newState))
        {
            ExitInvisibility();
        }
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || !UseCooldown)
            return;

        bool allowed = ShouldHide(currentState);
        if (!allowed)
        {
            ExitInvisibility();
            nextAvailableTime = Mathf.Max(nextAvailableTime, Time.time + 0.2f);
            return;
        }

        if (!isInvisible && Time.time >= nextAvailableTime)
        {
            EnterInvisibility();
        }
        else if (isInvisible && Time.time >= invisibilityEndTime)
        {
            ExitInvisibility();
            nextAvailableTime = Time.time + Mathf.Max(0.5f, CooldownDuration);
        }
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null) return;
        foreach (var r in renderers) if (r) r.enabled = visible;
        isInvisible = !visible;
    }

    private bool ShouldHide(EnemyState state)
    {
        return (state == EnemyState.Patrol && HideInPatrol)
            || (state == EnemyState.Chase && HideInChase)
            || (state == EnemyState.Attack && HideInAttack);
    }

    private void EnterInvisibility()
    {
        SetVisible(false);
        invisibilityEndTime = Time.time + InvisibleDuration;
    }

    private void ExitInvisibility()
    {
        if (isInvisible)
        {
            SetVisible(true);
        }
    }
}
