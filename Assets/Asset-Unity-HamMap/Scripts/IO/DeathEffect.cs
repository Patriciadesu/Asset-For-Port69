using UnityEngine;

public class DeathEffect : ObjectEffect
{
    public override void ApplyEffect(Collision playerCollision)
    {
        Player player = playerCollision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - Player respawned!");
        }
    }
    
    public override void ApplyEffect(Collision playerCollision, Player player)
    {
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - {player.gameObject.name} respawned!");
        }
    }
}