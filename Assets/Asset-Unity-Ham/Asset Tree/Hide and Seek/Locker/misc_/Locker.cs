using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public partial class Locker : MonoBehaviour
{
    [Header("Mode")]
    [Dropdown(nameof(GetAvailableModes))]
    [SerializeField] private LockerMode mode = LockerMode.Standard;

    [Header("References")]
    private Camera lockerCamera;
    private Transform lockerCameraPosition;
    [FormerlySerializedAs("Exitpos")]
    [SerializeField] private Transform exitPoint;

    private EntryPoint entryPoint;

    private GameObject player;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleHideKey = KeyCode.E;

    public static bool IsHiding { get; private set; }
    public static bool isHiding => IsHiding;
    public static Transform CurrentLocker { get; private set; }

    private void Awake()
    {
        if (!lockerCamera)
        {
            lockerCamera = GetComponentInChildren<Camera>(true);
        }

        if (!lockerCameraPosition && lockerCamera)
        {
            lockerCameraPosition = lockerCamera.transform;
        }

        CacheEntryPoint();
    }

    private void Start()
    {
        EnsurePlayerReference();
        CacheEntryPoint();

        if (lockerCamera && !lockerCameraPosition)
        {
            lockerCameraPosition = lockerCamera.transform;
        }

        if (lockerCamera)
        {
            lockerCamera.enabled = false;
        }

        OnStartExtra();
    }

    private void Update()
    {
        EnsureModeValidity();
        OnUpdateExtra();

        if (!entryPoint || !player) return;

        if (Input.GetKeyDown(toggleHideKey) && entryPoint.isInzone)
        {
            if (!IsHiding)
            {
                EnterLocker();
            }
            else
            {
                entryPoint.isInzone = false;
                ExitLocker();
            }
        }
    }

    private void EnterLocker()
    {
        EnsurePlayerReference();
        if (!player) return;

        OnBeforeEnterExtra();

        if (!exitPoint) exitPoint = transform;

        player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

        IsHiding = true;
        CurrentLocker = transform;
        LockerState.IsHiding = true;
        LockerState.CurrentLocker = transform;

        player.SetActive(false);

        if (lockerCamera)
        {
            lockerCamera.enabled = true;
            if (lockerCameraPosition)
            {
                lockerCamera.transform.SetPositionAndRotation(lockerCameraPosition.position, lockerCameraPosition.rotation);
            }
        }

        OnAfterEnterExtra();
    }

    private void ExitLocker()
    {
        if (!player) return;

        IsHiding = false;
        if (CurrentLocker == transform)
        {
            CurrentLocker = null;
        }
        LockerState.IsHiding = false;
        if (LockerState.CurrentLocker == transform)
        {
            LockerState.CurrentLocker = null;
        }

        if (exitPoint)
        {
            Vector3 forward = exitPoint.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
            }
            forward.Normalize();

            Vector3 faceDirection = -forward;
            if (faceDirection.sqrMagnitude < 0.0001f)
            {
                faceDirection = -transform.forward;
            }
            faceDirection.Normalize();

            player.transform.SetPositionAndRotation(exitPoint.position, Quaternion.LookRotation(faceDirection, Vector3.up));
        }

        player.SetActive(true);
        if (lockerCamera) lockerCamera.enabled = false;

        OnAfterExitExtra();
    }

    private void OnDisable()
    {
        if (IsHiding)
        {
            ExitLocker();
        }
        else
        {
            LockerState.IsHiding = false;
            if (LockerState.CurrentLocker == transform)
            {
                LockerState.CurrentLocker = null;
            }
        }

        OnDisableExtra();
    }

    private void EnsurePlayerReference()
    {
        if (player) return;
        if (Player.Instance)
        {
            player = Player.Instance.gameObject;
            return;
        }

        var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer)
        {
            player = taggedPlayer;
        }
    }

    private void CacheEntryPoint()
    {
        if (!entryPoint)
        {
            entryPoint = this.gameObject.GetComponentInChildren<EntryPoint>();
        }
    }

    // Hooks for optional modules (e.g., bait mode)
    partial void OnStartExtra();
    partial void OnUpdateExtra();
    partial void OnBeforeEnterExtra();
    partial void OnAfterEnterExtra();
    partial void OnAfterExitExtra();
    partial void OnDisableExtra();

    internal bool IsBaitMode => mode == LockerMode.Bait;

    private enum LockerMode
    {
        Standard,
        Bait
    }

    private DropdownList<LockerMode> GetAvailableModes()
    {
        var list = new DropdownList<LockerMode>
        {
            { "Standard", LockerMode.Standard }
        };

        AppendAdditionalModes(list);
        return list;
    }

    private void EnsureModeValidity()
    {
        if (mode != LockerMode.Standard && !IsModeSupported(mode))
        {
            mode = LockerMode.Standard;
        }
    }

    private bool IsModeSupported(LockerMode candidate)
    {
        if (candidate == LockerMode.Standard) return true;

        bool supported = false;
        CheckModeSupport(candidate, ref supported);
        return supported;
    }

    static partial void AppendAdditionalModes(DropdownList<LockerMode> modes);
    static partial void CheckModeSupport(LockerMode candidate, ref bool supported);
}

