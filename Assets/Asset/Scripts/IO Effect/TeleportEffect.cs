using UnityEngine;

public class TeleportEffect : ObjectEffect
{
    [SerializeField] private Vector3 teleportDestination;

    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = teleportDestination;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = teleportDestination;
            }

            Debug.Log($"{gameObject.name} teleported player to {teleportDestination}");
        }
    }
}