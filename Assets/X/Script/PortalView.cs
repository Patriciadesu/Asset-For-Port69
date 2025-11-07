using System.Linq;
using UnityEngine;
using Unity.Cinemachine;

public class PortalView : MonoBehaviour
{
    [Header("Portal Links")]
    public PortalView otherPortal;
    public Camera portalView;

    [Header("Visuals")]
    public Shader portalShader;
    [SerializeField] private MeshRenderer portalMesh;

    [Header("Player Camera Auto-Discovery")]
    [SerializeField] private Transform playerRootOverride;
    [SerializeField] private bool autoReacquire = true;

    // === Tuning (เดิม) ===
    [Header("Tuning")]
    [Range(0.1f, 2f)][SerializeField] private float distanceScale = 0.1f;
    [SerializeField] private float forwardBias = 0f;
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    // === RT Sizing ===
    [Header("RenderTexture Sizing")]
    [Tooltip("ความละเอียดด้านที่สั้นกว่า (อีกด้านจะคำนวณจากอัตราส่วนของกรอบพอร์ทัล)")]
    [SerializeField] private int rtShortSide = 1024;
    [Tooltip("rebuild RT เมื่อสัดส่วนกรอบเปลี่ยนเกินค่านี้")]
    [SerializeField] private float aspectEpsilon = 0.005f;

    [HideInInspector] public Camera playercam;

    private Material portalMaterial;
    private RenderTexture otherRT;

    // cache สำหรับ rebuild
    private float lastPortalAspect = -1f;

    void Awake()
    {
        if (!portalView)
        {
            portalView = GetComponentInChildren<Camera>(true);
            if (!portalView)
                Debug.LogError("[PortalView] portalView missing.");
        }
    }

    void Start()
    {
        BuildRTAndMaterial();      // สร้าง RT ให้ “พอดีกรอบ”
        TryAcquirePlayerCamera(true);
        SyncProjectionFromPlayer(); // sync FOV/aspect ครั้งแรก
    }

    void OnEnable()
    {
        if (!playercam) TryAcquirePlayerCamera(false);
        EnsureRTUpToDate(); // เผื่อเปิด/ปิดพรีแฟบแล้วสเกลเปลี่ยน
    }

    void Update()
    {
        if (autoReacquire && (!playercam || !playercam.isActiveAndEnabled))
            TryAcquirePlayerCamera(false);

        EnsureRTUpToDate();    // rebuild RT ถ้าสัดส่วนกรอบเปลี่ยน
        SyncProjectionFromPlayer();

        if (!playercam || !otherPortal || !portalView) return;

        // === Transform Mirror ===
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

    // ---------- Core: Build RT that matches the portal frame ----------
    void BuildRTAndMaterial()
    {
        float portalAspect = ComputePortalPlaneAspect();   // กว้าง/สูงของ “กรอบพอร์ทัล” จริงในซีน
        lastPortalAspect = portalAspect;

        int w, h;
        if (portalAspect >= 1f)
        {      // กว้างกว่าสูง
            h = rtShortSide;
            w = Mathf.Max(64, Mathf.RoundToInt(h * portalAspect));
        }
        else
        {                        // สูงกว่ากว้าง
            w = rtShortSide;
            h = Mathf.Max(64, Mathf.RoundToInt(w / portalAspect));
        }

        // สร้าง RT ใหม่ให้ตรงสัดส่วนกรอบ
        if (otherRT) { otherRT.Release(); Destroy(otherRT); }
        otherRT = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
        otherRT.name = $"PortalRT_{name}_From_{otherPortal?.name}";

        // กล้องของ "อีกฝั่ง" ต้องเรนเดอร์ใส่ RT นี้
        if (otherPortal && otherPortal.portalView)
            otherPortal.portalView.targetTexture = otherRT;

        // วัสดุของกรอบนี้ใช้ RT นี้
        if (portalMaterial == null)
        {
            portalMaterial = new Material(portalShader);
            portalMesh.material = portalMaterial;
        }
        portalMaterial.mainTexture = otherRT;

        // กล้องฝั่งเราให้ aspect ตาม RT (สอดคล้องกับกรอบ)
        if (portalView)
            portalView.aspect = (float)w / h;
    }

    void EnsureRTUpToDate()
    {
        float currentAspect = ComputePortalPlaneAspect();
        if (Mathf.Abs(currentAspect - lastPortalAspect) > aspectEpsilon)
            BuildRTAndMaterial();
    }

    // คำนวณอัตราส่วนกรอบจาก Mesh จริง (รองรับสเกล/หมุน)
    float ComputePortalPlaneAspect()
    {
        if (!portalMesh) return 1f;

        var mf = portalMesh.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return 1f;

        var b = mf.sharedMesh.bounds;
        var t = portalMesh.transform;

        // สร้าง 8 มุม แล้ว project ลงแกน right/up ของกรอบ
        Vector3 c = b.center;
        Vector3 e = b.extents;
        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
                for (int zi = -1; zi <= 1; zi += 2)
                    corners[i++] = t.TransformPoint(c + Vector3.Scale(e, new Vector3(xi, yi, zi)));

        Vector3 right = t.right;
        Vector3 up = t.up;

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
        Transform root = playerRootOverride;
        if (!root)
        {
            var playerInst = Player.Instance;
            if (playerInst) root = playerInst.transform;
        }
        if (!root)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) root = tagged.transform;
        }
        if (!root)
        {
            if (verboseLog) Debug.LogWarning("[PortalView] Player root not found.");
            return;
        }

        var allPlayerCams = root.GetComponentsInChildren<Camera>(true)
            .Where(c => c && c != portalView && (otherPortal == null || c != otherPortal.portalView))
            .ToList();

        if (allPlayerCams.Count == 0)
        {
            if (verboseLog) Debug.LogWarning("[PortalView] No cameras under Player.");
            return;
        }

        Camera pick = allPlayerCams.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null && c.isActiveAndEnabled)
                   ?? allPlayerCams.FirstOrDefault(c => c.GetComponent<CinemachineBrain>() != null)
                   ?? allPlayerCams.Where(c => c.isActiveAndEnabled).OrderBy(c => c.depth).LastOrDefault()
                   ?? allPlayerCams[0];

        playercam = pick;
        if (verboseLog && playercam)
            Debug.Log($"[PortalView] Bound player camera => {playercam.name} (depth={playercam.depth})", playercam);
    }

    // ให้มุมมองเท่ากล้องผู้เล่น (กันอาการ “ซูม/บานปลาย”)
    void SyncProjectionFromPlayer()
    {
        if (!playercam || !portalView) return;

        portalView.orthographic = playercam.orthographic;
        if (playercam.orthographic)
            portalView.orthographicSize = playercam.orthographicSize;
        else
            portalView.fieldOfView = playercam.fieldOfView;

        // aspect ของ portalView ถูกตั้งตอน BuildRT แล้วให้ตรงกรอบ
        // หากอยากบังคับให้ตามผู้เล่นแทน ให้ใช้:
        // portalView.aspect = playercam.aspect;
    }

    void OnDestroy()
    {
        if (otherRT)
        {
            if (otherPortal && otherPortal.portalView && otherPortal.portalView.targetTexture == otherRT)
                otherPortal.portalView.targetTexture = null;
            otherRT.Release();
            Destroy(otherRT);
        }
        if (portalMaterial) Destroy(portalMaterial);
    }
}
