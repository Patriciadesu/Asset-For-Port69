using System.Reflection;
using UnityEngine;

public static class PortalFacingUtils
{
    private const float MinPlanarMagnitude = 0.0001f;
    private static readonly FieldInfo TpsYawField =
        typeof(CameraModule).GetField("tpsYaw", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TpsPitchField =
        typeof(CameraModule).GetField("tpsPitch", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void AlignPlayerFacingOut(Transform teleportedTransform, Transform exitPortal)
    {
        if (!teleportedTransform || !exitPortal) return;

        Player player = teleportedTransform.GetComponent<Player>() ?? teleportedTransform.GetComponentInParent<Player>();
        if (!player) return;

        Vector3 planarForward = Vector3.ProjectOnPlane(exitPortal.forward, Vector3.up);
        if (planarForward.sqrMagnitude < MinPlanarMagnitude)
        {
            planarForward = exitPortal.forward;
        }

        if (planarForward.sqrMagnitude < MinPlanarMagnitude) return;

        Quaternion desiredRotation = Quaternion.LookRotation(planarForward.normalized, Vector3.up);

        if (player.rigidbody)
        {
            player.rigidbody.rotation = desiredRotation;
        }

        player.transform.rotation = desiredRotation;
        teleportedTransform.rotation = desiredRotation;

        SyncCameraModule(player, desiredRotation);
    }

    private static void SyncCameraModule(Player player, Quaternion worldRotation)
    {
        var camModule = player.Cam;
        if (camModule == null) return;

        if (camModule.cameraType == CameraType.ThirdPerson)
        {
            float yaw = worldRotation.eulerAngles.y;
            TpsYawField?.SetValue(camModule, yaw);

            float pitch = 0f;
            if (player.tpsCameraPivot)
            {
                pitch = NormalizePitch(player.tpsCameraPivot.rotation.eulerAngles.x);
                player.tpsCameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            TpsPitchField?.SetValue(camModule, pitch);
        }
        else if (camModule.cameraType == CameraType.FirstPerson && player.fpsCameraPivot)
        {
            player.fpsCameraPivot.rotation = Quaternion.identity;
        }
    }

    private static float NormalizePitch(float raw)
    {
        if (raw > 180f) raw -= 360f;
        return Mathf.Clamp(raw, -20f, 60f);
    }
}

