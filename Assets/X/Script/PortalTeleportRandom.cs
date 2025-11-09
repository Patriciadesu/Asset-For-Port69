using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DoorScript;   // class Door ของคุณ

[RequireComponent(typeof(Collider))]
public class PortalTeleportRandom : MonoBehaviour
{
    [Header("Network / Grouping")]
    public string group = "Default";
    public bool randomizeOnStart = true;
    public bool randomizeEveryOpen = true;
    public int avoidImmediateRepeat = 1;

    [Header("References (optional; จะ auto-resolve ถ้าเว้นว่าง)")]
    [Tooltip("ถ้าเว้นว่าง ระบบจะหาจาก Parent/Children อัตโนมัติ")]
    [SerializeField] private Door door;                   // <-- Door อยู่บนลูกได้
    [Tooltip("ทรานส์ฟอร์มที่ใช้เป็น 'ระนาบพอร์ทัล' สำหรับคณิต mirror (ไม่ใส่ = ใช้ตัวเอง)")]
    [SerializeField] private Transform portalPlane;       // <-- ชี้ไปที่กรอบประตูจริง (เช่น child 'Door')
    [Tooltip("ตัว PortalView ของพอร์ทัลนี้ (ไม่ใส่ = หาใน Parent/Children)")]
    [SerializeField] private PortalView portalView;

    [Header("Filter")]
    public string playerTag = "Player";
    public LayerMask teleportLayers = ~0;

    [Header("Stability")]
    public float reentryCooldown = 0.12f;
    public float exitForwardPush = 0.2f;

    [Header("Runtime (read-only)")]
    [SerializeField] private PortalTeleportRandom currentTarget;
    [SerializeField] private List<PortalTeleportRandom> poolPreview = new();

    private Collider trigger;
    private bool lastOpen = false;
    private readonly Dictionary<Transform, float> nextAllowed = new();

    private static readonly Dictionary<string, List<PortalTeleportRandom>> registry
        = new Dictionary<string, List<PortalTeleportRandom>>();

    void OnEnable()
    {
        trigger = GetComponent<Collider>();
        if (trigger && !trigger.isTrigger) trigger.isTrigger = true;

        // --- Resolve refs แบบยืดหยุ่น ---
        if (!door) door = GetComponentInChildren<Door>(true) ?? GetComponentInParent<Door>();
        if (!portalPlane) portalPlane = transform; // ถ้าไม่ได้เซ็ต ให้ใช้ตัวเองเป็นระนาบ
        if (!portalView) portalView = GetComponentInChildren<PortalView>(true) ?? GetComponentInParent<PortalView>();

        RegisterIntoGroup();

        lastOpen = door ? door.open : false;
        if (randomizeOnStart) PickNewTarget();
    }

    void OnDisable() => UnregisterFromGroup();

    void Update()
    {
        // สุ่มใหม่เมื่อสถานะ "ปิด → เปิด"
        if (randomizeEveryOpen && door && door.open && !lastOpen)
            PickNewTarget();

        lastOpen = door ? door.open : lastOpen;
    }

    // ---------- Group registry ----------
    void RegisterIntoGroup()
    {
        if (!registry.TryGetValue(group, out var list))
        {
            list = new List<PortalTeleportRandom>();
            registry[group] = list;
        }
        if (!list.Contains(this)) list.Add(this);
        poolPreview = list;
    }
    void UnregisterFromGroup()
    {
        if (registry.TryGetValue(group, out var list)) list.Remove(this);
    }

    // ---------- Random target pick ----------
    public void PickNewTarget()
    {
        if (!registry.TryGetValue(group, out var list)) return;

        var candidates = list.Where(p => p && p != this).ToList();
        if (candidates.Count == 0) { currentTarget = null; return; }

        var pick = candidates[Random.Range(0, candidates.Count)];
        if (avoidImmediateRepeat > 0 && currentTarget && candidates.Count > 1)
        {
            int tries = 8;
            while (pick == currentTarget && tries-- > 0)
                pick = candidates[Random.Range(0, candidates.Count)];
        }

        currentTarget = pick;

        var myPV = portalView ?? GetComponentInChildren<PortalView>(true) ?? GetComponentInParent<PortalView>();
        var targetPV = currentTarget
            ? (currentTarget.portalView ?? currentTarget.GetComponentInChildren<PortalView>(true) ?? currentTarget.GetComponentInParent<PortalView>())
            : null;

        if (myPV && targetPV)
        {
            myPV.otherPortal = targetPV;
            myPV.ForceRebindRT();     // <<— สำคัญ: rebind RT หลังเปลี่ยนปลายทาง
        }
    }

    // ---------- Teleport ----------
    void OnTriggerStay(Collider other)
    {
        if (!currentTarget) return;
        if (!other.CompareTag(playerTag)) return;
        if ((teleportLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (nextAllowed.TryGetValue(other.transform, out var t) && Time.time < t) return;

        // ใช้ระนาบของ portalPlane (ไม่จำเป็นต้องตรงกับตัวที่มีสคริปต์)
        float localZ = portalPlane.InverseTransformPoint(other.bounds.center).z;
        if (localZ < 0f)
        {
            DoTeleport(other.transform, other.attachedRigidbody);
            float until = Time.time + reentryCooldown;
            nextAllowed[other.transform] = until;
            currentTarget.nextAllowed[other.transform] = until;
        }
    }

    void DoTeleport(Transform target, Rigidbody rb)
    {
        StartCoroutine(TeleportUtils.PausePlayer(target, 0.06f));

        // ใช้ระนาบ/ทรงของทั้งสองบาน
        var toPlane = currentTarget.portalPlane ? currentTarget.portalPlane : currentTarget.transform;

        Vector3 local = portalPlane.InverseTransformPoint(target.position);
        local = new Vector3(-local.x, local.y, -local.z);

        Vector3 exitWorld = toPlane.TransformPoint(local) + toPlane.forward * exitForwardPush;

        Quaternion delta = toPlane.rotation *
                           Quaternion.Inverse(portalPlane.rotation * Quaternion.Euler(0, 180, 0));

        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = delta * rb.linearVelocity;
#else
            rb.velocity        = delta * rb.velocity;
#endif
            rb.angularVelocity = delta * rb.angularVelocity;

            rb.position = exitWorld;
            rb.rotation = delta * rb.rotation;
            rb.WakeUp();
        }
        else
        {
            target.SetPositionAndRotation(exitWorld, delta * target.rotation);
        }
    }
}
