using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Look Input")]
    [Tooltip("If true, the camera only rotates while a mouse button is held (drag to look).")]
    [SerializeField] private bool requireMouseDragForLook = true;

    [Tooltip("Mouse button index used for drag look: 0 = left, 1 = right, 2 = middle.")]
    [SerializeField] private int dragMouseButton = 1;

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

    private bool IsTouchOverUI(Vector2 position, int fingerId)
    {
        // First check with EventSystem
        if (EventSystem.current != null)
        {
            // For touch input
            if (fingerId >= 0)
            {
                if (EventSystem.current.IsPointerOverGameObject(fingerId))
                    return true;
            }
            // For mouse input
            else
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return true;
            }
        }

        // Additional raycast check for UI elements (more reliable in simulator)
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Check if any UI element was hit
        foreach (var result in results)
        {
            if (result.gameObject.layer == LayerMask.NameToLayer("UI") || 
                result.gameObject.GetComponent<UnityEngine.UI.Graphic>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleMouseLook()
    {
        if (!Player.Instance.canRotateCamera) return;

        float mouseX = 0f;
        float mouseY = 0f;

        // Check for touchscreen input
        if (Input.touchCount > 0)
        {
            // In Unity Editor/Simulator: allow single touch for testing
            // On real mobile devices: require 2 touches (joystick + camera drag)
            #if !UNITY_EDITOR
            if (Input.touchCount < 2)
            {
                return; // Don't move camera with single touch on real devices
            }
            #endif

            bool hasValidTouch = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Skip if touching UI (e.g. Joystick)
                if (IsTouchOverUI(touch.position, touch.fingerId))
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    hasValidTouch = true;
                    // Scale pixel delta to match mouse sensitivity feel
                    float touchFactor = 0.1f; 
                    mouseX += touch.deltaPosition.x * mouseSensitivity * touchFactor;
                    mouseY += touch.deltaPosition.y * mouseSensitivity * touchFactor;
                }
            }

            // In editor with single touch: only move camera if not touching UI
            #if UNITY_EDITOR
            if (Input.touchCount == 1 && !hasValidTouch)
            {
                return; // Single touch was on UI (joystick), don't move camera
            }
            #endif
        }
        else if (Input.GetMouseButton(0)) // Fallback for Editor/Mouse
        {
            if (IsTouchOverUI(Input.mousePosition, -1))
            {
                // Over UI, ignore
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            }
        }

        if (Mathf.Abs(mouseX) < 0.001f && Mathf.Abs(mouseY) < 0.001f) return;

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
