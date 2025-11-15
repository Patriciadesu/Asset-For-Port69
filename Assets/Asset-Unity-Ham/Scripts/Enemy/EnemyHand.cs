using UnityEngine;

/// <summary>
/// Simple marker component to indicate the enemy's hand / weapon tip.
/// ProjectileModule and BeamModule will search for this and use its transform
/// as FirePoint / BeamOrigin automatically. This script does NOT depend on any modules.
/// </summary>
[AddComponentMenu("Killer AI/Helpers/Enemy Hand")]
public class EnemyHand : MonoBehaviour
{
    // Intentionally empty – acts only as a tag/marker.
}
