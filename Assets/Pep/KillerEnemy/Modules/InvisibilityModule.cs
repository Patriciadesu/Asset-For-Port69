using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Invisibility Module")]
[Tooltip("Toggles renderers on state changes for simple invisibility.")]
public class InvisibilityModule : EnemyModule
{
    [Header("Invisibility Settings")]
    public bool HideInPatrol = false;
    public bool HideInChase = false;
    public bool HideInAttack = false;

    private Renderer[] renderers;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        bool hide = (newState == EnemyState.Patrol && HideInPatrol)
                 || (newState == EnemyState.Chase && HideInChase)
                 || (newState == EnemyState.Attack && HideInAttack);
        SetVisible(!hide);
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null) return;
        foreach (var r in renderers) if (r) r.enabled = visible;
    }
}
