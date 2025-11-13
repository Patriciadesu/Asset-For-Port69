using UnityEngine;
using NaughtyAttributes;
using Unity.Cinemachine;

[System.Serializable]
public class CameraModule : PlayerModule
{
    // Config
    public float mouseSensitivity = 2f;
    public CameraType cameraType = CameraType.ThirdPerson;

    [AllowNesting, SerializeField, Range(30, 120)]
    private float cameraFOV = 60f;

    [AllowNesting, ShowIf("cameraType", CameraType.ThirdPerson), Range(0, 1), SerializeField]
    private float cameraSide = 0.5f;

    [AllowNesting, ShowIf("cameraType", CameraType.ThirdPerson), SerializeField]
    private float cameraDistance = 5f;

    [AllowNesting, ShowIf("cameraType", CameraType.ThirdPerson), Range(-1, 2), SerializeField]
    private float yOffset = -0.4f;

    // State
    private float xRotation = 0f;
    private float tpsYaw = 0f;
    private float tpsPitch = 10f;

    // Shortcuts (null-safe)
    private Camera Cam    => player?.camera;
    private Transform FpsPivot => player?.fpsCameraPivot;
    private Camera TpsCam => player?.tpsCamera;
    private CinemachineThirdPersonFollow TpsFollow => player?.tpsVirtualCamera;
    private Transform TpsPivot => player?.tpsCameraPivot;

    public CameraModule(Player owner) : base(owner) { player = owner; }

    public override void Start()
    {
        base.Start();
        if (!enableModule || player == null) return;
        ApplyRigSettings();
        PositionFromRig();
    }

    public override void OnValidate()
    {
        base.OnValidate();
        ApplyRigSettings();
    }

    public override void Update()
    {
        base.Update();
        if (!Application.isPlaying || player == null) return;

        HandleMouseLook();
        PositionFromRig();
    }

    private void ApplyRigSettings()
    {
        if (player == null)
        {
            Debug.Log("Camera Module : Player is null");
            return;
        }

        switch (cameraType)
        {
            case CameraType.FirstPerson:
                if (TpsCam) TpsCam.gameObject.SetActive(false);
                if (Cam)
                {
                    Cam.gameObject.SetActive(true);
                    Cam.fieldOfView = cameraFOV;
                }
                break;

            case CameraType.ThirdPerson:
                if (Cam) Cam.gameObject.SetActive(false);
                if (TpsCam) TpsCam.gameObject.SetActive(true);
                if (TpsFollow != null)
                {
                    var vcam = TpsFollow.GetComponent<CinemachineCamera>();
                    if (vcam != null)
                        vcam.Lens.FieldOfView = cameraFOV;

                    TpsFollow.CameraDistance = cameraDistance;
                    TpsFollow.CameraSide     = cameraSide;
                    var off = TpsFollow.ShoulderOffset;
                    off.y = yOffset;
                    TpsFollow.ShoulderOffset = off;
                }
                break;
        }
    }

    private void PositionFromRig()
    {
        if (cameraType == CameraType.FirstPerson)
        {
            if (Cam && FpsPivot)
                Cam.transform.position = FpsPivot.position;
        }
        // TPS is driven by Cinemachine rig; nothing to place manually.
    }

    private void HandleMouseLook()
    {
        if (!Player.Instance.canRotateCamera) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (cameraType == CameraType.FirstPerson)
        {
            if (player == null || Cam == null) return;

            player.transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            Cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else // ThirdPerson
        {
            if (TpsPivot == null || player == null) return;

            tpsYaw   += mouseX;
            tpsPitch -= mouseY;
            tpsPitch  = Mathf.Clamp(tpsPitch, -20f, 60f);

            TpsPivot.rotation       = Quaternion.Euler(tpsPitch, tpsYaw, 0f);
            player.transform.rotation= Quaternion.Euler(0f, tpsYaw, 0f);
        }
    }
}
