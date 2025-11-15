using System.Collections;
using System.Linq;
using DoorScript;
using UnityEngine;
using Unity.Cinemachine;

public class PortalView : MonoBehaviour
{
    [Header("Portal Links")]
    [HideInInspector] public PortalView otherPortal;
    private Camera portalView;

    [Header("Visuals")]
    [SerializeField] private Shader portalShader;
    private MeshRenderer portalMesh;

    [Header("Player Camera Auto-Discovery")]
    private Transform playerRootOverride => Player.Instance ? Player.Instance.transform : null;
    private bool autoReacquire = true;
    [Tooltip("หน่วงก่อน rebind เมื่อกล้องผู้เล่นถูกปิดชั่วคราว (เช่น ตอนสลับ vcam)")]
    private int reacquireGraceFrames = 6;
    [Tooltip("พยายามใช้ Player.Instance.camera ก่อนวิธีอื่น")]
    private bool preferPlayerInstanceCamera = true;

    [Header("Tuning")]
    private float distanceScale = 1f;
    private float forwardBias = 0f;
    private Vector3 localOffset = Vector3.zero;
    [SerializeField, Tooltip("If true, remote view sticks to the destination portal's arrival anchor.")]
    private bool lockCameraToArrivalAnchor = true;
    [SerializeField, Tooltip("When false, copy camera pose instead of mirror.")]
    private bool mirrorWhenUnlocked = false;
    [SerializeField, Tooltip("Keep the partner door open/closed in sync with this door.")]
    private bool syncDoorStates = true;

    [Header("RenderTexture Sizing")]
    private int rtShortSide = 1024;
    private float aspectEpsilon = 0.005f;

    [Header("Safety")]
    [Tooltip("ตัดกล้องที่อยู่ใต้ PortalView หรือมีกำหนด targetTexture (กล้องพอร์ทัล)")]
    private bool excludePortalCameras = true;
    [Tooltip("Rebind RT อัตโนมัติเมื่อ otherPortal เปลี่ยน")]
    private bool autoRebindOnOtherChange = true;

    [HideInInspector] public Camera playercam;

    private Material portalMaterial;
    private RenderTexture otherRT;
    private float lastPortalAspect = -1f;

    // track state เพื่อ rebind/กันแกว่ง
    private int lostUsableFrames = 0;
    private PortalView _lastOther;
    private PortalTeleport cachedOtherTeleport;
    private Transform cachedArrivalAnchor;
    private Door cachedOwnDoor;
    private Door cachedOtherDoor;

    void Awake()
    {
        if (!portalView)
            portalView = GetComponentInChildren<Camera>(true);
        portalMesh = FindPortalMesh();
        EnsurePortalShader();
        cachedOwnDoor = GetComponentInParent<Door>(true);
    }

    private MeshRenderer FindPortalMesh()
    {
        MeshRenderer result = null;
        var tagged = this.gameObject.GetComponentsInChildren<Transform>(true)
                     .Select(t => t.gameObject)
                     .FirstOrDefault(child => child.CompareTag("PortalMesh"));
        if (tagged)
        {
            result = tagged.GetComponent<MeshRenderer>();
        }
        if (!result)
        {
            result = GetComponentInChildren<MeshRenderer>(true);
        }
        return result;
    }

    private void EnsurePortalShader()
    {
        if (portalShader) return;
        portalShader = Shader.Find("Unlit/PortalShader");
        if (!portalShader)
        {
            portalShader = Shader.Find("Unlit/Texture");
            if (!portalShader)
            {
                Debug.LogWarning("[PortalView] Unable to find fallback portal shader. Portal preview will be blank.");
            }
        }
    }

    void Start()
    {
        BuildRTAndMaterial();
        // หน่วงให้ระบบกล้องนิ่งก่อนค่อย bind
        StartCoroutine(BindLate());
    }

    IEnumerator BindLate()
    {
        // รออย่างน้อย 1 frame + 1 fixed เพื่อให้ Cinemachine/โมดูลกล้องของ Player set เสร็จ
        yield return null;
        yield return new WaitForFixedUpdate();

        TryAcquirePlayerCamera(true);
        SyncProjectionFromPlayer();
        ForceRebindRT();
    }

    void OnEnable()
    {
        if (playercam == null)
            StartCoroutine(BindLate());
        else
            ForceRebindRT();
    }

    void Update()
    {
        EnsureRTUpToDate();

        // Rebind RT อัตโนมัติเมื่อปลายทางเปลี่ยน
        if (autoRebindOnOtherChange && otherPortal != _lastOther)
            ForceRebindRT();

        // Reacquire แบบมี grace เมื่อกล้องผู้เล่น “ไม่พร้อมใช้งาน” ชั่วคราว
        if (autoReacquire)
        {
            if (!IsUsablePlayerCamera(playercam))
            {
                lostUsableFrames++;
                if (lostUsableFrames >= reacquireGraceFrames)
                {
                    TryAcquirePlayerCamera(false);
                    lostUsableFrames = 0;
                }
            }
            else lostUsableFrames = 0;
        }

        if (!playercam || !portalView) return;

        if (lockCameraToArrivalAnchor && cachedArrivalAnchor)
        {
            portalView.transform.SetPositionAndRotation(cachedArrivalAnchor.position, cachedArrivalAnchor.rotation);
            portalView.nearClipPlane = 0.05f;
        }
        else
        {
            if (!otherPortal) return;
            if (mirrorWhenUnlocked)
                UpdateMirrorCamera();
            else
                UpdateAlignedCamera();
        }

        SyncDoorState();
    }

    private void UpdateMirrorCamera()
    {
        Vector3 lp = otherPortal.transform.worldToLocalMatrix.MultiplyPoint3x4(playercam.transform.position);
        Vector3 mirrored = new Vector3(-lp.x, lp.y, -lp.z);

        Vector3 adjusted = mirrored * distanceScale
                         + Vector3.forward * forwardBias
                         + localOffset;

        portalView.transform.localPosition = adjusted;

        Quaternion difference =
            transform.rotation * Quaternion.Inverse(otherPortal.transform.rotation * Quaternion.Euler(0, 180, 0));
        portalView.transform.rotation = difference * playercam.transform.rotation;

        float dist = adjusted.magnitude;
        portalView.nearClipPlane = Mathf.Clamp(dist - 0.02f, 0.03f, 0.3f);
    }

    private void UpdateAlignedCamera()
    {
        Vector3 lp = otherPortal.transform.worldToLocalMatrix.MultiplyPoint3x4(playercam.transform.position);
        Vector3 adjusted = lp * distanceScale
                         + Vector3.forward * forwardBias
                         + localOffset;

        portalView.transform.localPosition = adjusted;

        Quaternion difference =
            transform.rotation * Quaternion.Inverse(otherPortal.transform.rotation);
        portalView.transform.rotation = difference * playercam.transform.rotation;

        float dist = adjusted.magnitude;
        portalView.nearClipPlane = Mathf.Clamp(dist - 0.02f, 0.03f, 0.3f);
    }

    private void SyncDoorState()
    {
        if (!syncDoorStates || !cachedOwnDoor) return;

        if (!cachedOtherDoor && cachedOtherTeleport)
        {
            cachedOtherDoor = cachedOtherTeleport.AssociatedDoor;
        }
        if (!cachedOtherDoor && otherPortal)
        {
            cachedOtherDoor = otherPortal.GetComponentInParent<Door>();
        }

        if (cachedOtherDoor)
        {
            cachedOtherDoor.open = cachedOwnDoor.open;
        }
    }

    // ---------- RT build/rebind ----------
    void BuildRTAndMaterial()
    {
        float portalAspect = ComputePortalPlaneAspect();
        lastPortalAspect = portalAspect;

        int w, h;
        if (portalAspect >= 1f) { h = rtShortSide; w = Mathf.Max(64, Mathf.RoundToInt(h * portalAspect)); }
        else { w = rtShortSide; h = Mathf.Max(64, Mathf.RoundToInt(w / portalAspect)); }

        if (otherRT) { otherRT.Release(); Destroy(otherRT); }
        otherRT = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
        otherRT.name = $"PortalRT_{name}_From_{otherPortal?.name}";

        EnsurePortalMaterial();
        if (portalMaterial)
        {
            portalMaterial.mainTexture = otherRT;
        }

        if (portalView) portalView.aspect = (float)w / h;

        // ให้แน่ใจว่า targetTexture ของ "กล้องอีกฝั่ง" ถูกชี้เข้ามาที่ RT ใหม่นี้
        ForceRebindRT();
    }

    public void ForceRebindRT()
    {
        if (otherRT == null) BuildRTAndMaterial();

        // ถอดของเก่าที่ชี้อยู่
        if (_lastOther && _lastOther.portalView && _lastOther.portalView.targetTexture == otherRT)
            _lastOther.portalView.targetTexture = null;

        // ผูกของใหม่
        if (otherPortal && otherPortal.portalView)
            otherPortal.portalView.targetTexture = otherRT;

        _lastOther = otherPortal;
        CacheOtherPortalData();
    }

    private void EnsurePortalMaterial()
    {
        if (!portalMesh) return;

        if (portalMaterial == null)
        {
            portalMaterial = portalMesh.material;
        }

        if (portalMaterial != null && portalMaterial.HasProperty("_MainTex"))
        {
            portalMesh.material = portalMaterial;
            return;
        }

        EnsurePortalShader();
        if (!portalShader) return;

        portalMaterial = new Material(portalShader);
        portalMesh.material = portalMaterial;
    }

    private void CacheOtherPortalData()
    {
        cachedOtherTeleport = otherPortal ? otherPortal.GetComponentInParent<PortalTeleport>() : null;
        cachedArrivalAnchor = cachedOtherTeleport ? cachedOtherTeleport.ArrivalAnchor : null;
        cachedOtherDoor = cachedOtherTeleport ? cachedOtherTeleport.AssociatedDoor
            : otherPortal ? otherPortal.GetComponentInParent<Door>() : null;
    }

    void EnsureRTUpToDate()
    {
        float currentAspect = ComputePortalPlaneAspect();
        if (Mathf.Abs(currentAspect - lastPortalAspect) > aspectEpsilon)
            BuildRTAndMaterial();
    }

    float ComputePortalPlaneAspect()
    {
        if (!portalMesh) return 1f;
        var mf = portalMesh.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return 1f;

        var b = mf.sharedMesh.bounds;
        var t = portalMesh.transform;

        Vector3 c = b.center, e = b.extents;
        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
                for (int zi = -1; zi <= 1; zi += 2)
                    corners[i++] = t.TransformPoint(c + Vector3.Scale(e, new Vector3(xi, yi, zi)));

        Vector3 right = t.right, up = t.up;
        float minR = float.PositiveInfinity, maxR = float.NegativeInfinity;
        float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;

        foreach (var p in corners)
        {
            float r = Vector3.Dot(p, right);
            float u = Vector3.Dot(p, up);
            if (r < minR) minR = r; if (r > maxR) maxR = r;
            if (u < minU) minU = u; if (u > maxU) maxU = u;
        }
        float width = Mathf.Max(0.0001f, maxR - minR);
        float height = Mathf.Max(0.0001f, maxU - minU);
        return width / height;
    }

    // ---------- Camera discovery ----------
    void TryAcquirePlayerCamera(bool verboseLog)
    {
        Camera pick = null;

        // A) ใช้ Player.Instance.camera ถ้ามีและ usable
        if (preferPlayerInstanceCamera && Player.Instance && IsUsablePlayerCamera(Player.Instance.camera))
            pick = Player.Instance.camera;

        // B) หาในรากผู้เล่น
        if (!pick)
        {
            Transform root = playerRootOverride;
            if (!root && Player.Instance) root = Player.Instance.transform;
            if (!root)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged) root = tagged.transform;
            }

            if (root)
            {
                var cams = root.GetComponentsInChildren<Camera>(true)
                               .Where(IsUsablePlayerCamera).ToList();
                // Priority: มี CinemachineBrain → เลือกตัวนั้น
                pick = cams.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null && c.isActiveAndEnabled)
                    ?? cams.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null)
                    ?? cams.OrderBy(c => c.depth).LastOrDefault();
            }
        }

        // C) Fallback: กล้องใดๆ ในซีนที่เป็น "usable" และมี CinemachineBrain
        if (!pick)
        {
            var all = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                            .Where(IsUsablePlayerCamera).ToList();
            pick = all.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null && c.isActiveAndEnabled)
                ?? all.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null);
        }

        // D) ฟางเส้นสุดท้าย: usable camera ตัวแรก (ยังคงไม่เอากล้องพอร์ทัล)
        if (!pick)
        {
            pick = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                         .FirstOrDefault(IsUsablePlayerCamera);
        }

        if (pick)
        {
            playercam = pick;
            if (verboseLog)
                Debug.Log($"[PortalView] Bound player camera => {playercam.name}", playercam);
        }
        else if (verboseLog)
        {
            Debug.LogWarning("[PortalView] Cannot find a usable player camera.");
        }
    }

    bool IsUsablePlayerCamera(Camera c)
    {
        if (!c) return false;
        if (!c.isActiveAndEnabled) return false;

        if (!excludePortalCameras) return true;

        // กล้องพอร์ทัลมักจะมี targetTexture หรืออยู่ใต้ PortalView
        bool isPortalCam = (c.targetTexture != null) ||
                           (c.GetComponentInParent<PortalView>(true) != null && c != playercam);
        return !isPortalCam;
    }

    // ---------- Projection sync ----------
    void SyncProjectionFromPlayer()
    {
        if (!playercam || !portalView) return;

        portalView.orthographic = playercam.orthographic;
        if (playercam.orthographic) portalView.orthographicSize = playercam.orthographicSize;
        else portalView.fieldOfView = playercam.fieldOfView;
    }

    void OnDestroy()
    {
        if (otherRT)
        {
            if (_lastOther && _lastOther.portalView && _lastOther.portalView.targetTexture == otherRT)
                _lastOther.portalView.targetTexture = null;
            otherRT.Release();
            Destroy(otherRT);
        }
        if (portalMaterial) Destroy(portalMaterial);
    }
}
