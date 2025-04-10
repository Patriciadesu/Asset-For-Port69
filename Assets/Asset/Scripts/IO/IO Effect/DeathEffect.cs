using UnityEngine;

public class DeathEffect : ObjectEffect
{
    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - Player respawned!");
        }
    }
}