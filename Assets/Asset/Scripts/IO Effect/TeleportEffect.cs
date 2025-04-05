using UnityEngine;

public class TeleportEffect : ObjectEffect
{
    public enum TeleportType
    {
        ToPosition,
        ToObject,
        OffsetFromCurrentPosition
    }

    [SerializeField] private TeleportType teleportType = TeleportType.ToPosition;

    [SerializeField] private Vector3 teleportDestination;
    [SerializeField] private Transform targetObject;
    [SerializeField] private Vector3 offset;

    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector3 finalPosition = player.transform.position;

            switch (teleportType)
            {
                case TeleportType.ToPosition:
                    finalPosition = teleportDestination;
                    break;
                case TeleportType.ToObject:
                    if (targetObject != null)
                        finalPosition = targetObject.position;
                    break;
                case TeleportType.OffsetFromCurrentPosition:
                    finalPosition += offset;
                    break;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = finalPosition;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = finalPosition;
            }

            Debug.Log($"{gameObject.name} teleported player to {finalPosition}");
        }
    }
}
