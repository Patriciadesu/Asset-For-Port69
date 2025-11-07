using UnityEngine;

public class PortalTeleport : MonoBehaviour
{
    public PortalTeleport otherTeleport;

    private void OnTriggerStay(Collider other)
    {
        float zPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(other.transform.position).z;

        if (zPosition < 0 && other.CompareTag("Player"))
        {
            Teleport(other.transform);
        }
    }

    void Teleport(Transform objectToTeleport)
    {
        Debug.Log("Teleport");

        StartCoroutine(TeleportUtils.PausePlayer(objectToTeleport, 0.05f));

        Vector3 localPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(objectToTeleport.position);
        localPosition = new Vector3(-localPosition.x, localPosition.y, -localPosition.z);
        objectToTeleport.position = otherTeleport.transform.localToWorldMatrix.MultiplyPoint3x4(localPosition);

        Quaternion diffrence = otherTeleport.transform.rotation * Quaternion.Inverse(transform.rotation * Quaternion.Euler(0,180,0));
        objectToTeleport.rotation = diffrence * objectToTeleport.rotation;
    }
}
