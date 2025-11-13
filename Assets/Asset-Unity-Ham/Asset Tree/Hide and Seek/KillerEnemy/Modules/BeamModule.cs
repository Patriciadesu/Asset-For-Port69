using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Beam Module")]
[Tooltip("Hitscan beam that deals damage to the player.")]
public class BeamModule : EnemyModule
{
    [Header("Beam Settings")]
    [Tooltip("Maximum range of the beam")]
    public float Range = 20f;

    [Tooltip("Amount of damage dealt by the beam")]
    public float BeamDamage = 20f;

    [Tooltip("Fire beam when entering attack state")]
    public bool FireOnAttackEnter = false;

    [Tooltip("Layer mask for beam raycasting")]
    public LayerMask HitLayers = -1; // All layers by default

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (FireOnAttackEnter && newState == EnemyState.Attack)
        {
            FireBeam();
        }
    }

    public void FireBeam()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = transform.forward;
        
        Debug.DrawRay(origin, dir * Range, Color.magenta, 0.5f);
        
        // Raycast to detect hits
        if (Physics.Raycast(origin, dir, out RaycastHit hit, Range, HitLayers))
        {
            // Check if we hit the player
            Player player = Player.Instance;
            if (player != null && player.Stat != null)
            {
                player.Stat.TakeDamage(BeamDamage);
                Debug.Log($"[BeamModule] Beam hit player for {BeamDamage} damage!");
            }
            else
            {
                Debug.Log($"[BeamModule] Beam hit but no player or stat found");
            }
        }
        else
        {
            Debug.Log("[BeamModule] Beam missed");
        }
    }
}
