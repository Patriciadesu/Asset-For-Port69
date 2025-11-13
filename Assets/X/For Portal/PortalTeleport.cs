using System.Collections.Generic;
using UnityEngine;
using DoorScript;

[RequireComponent(typeof(Collider))]
public partial class PortalTeleport : MonoBehaviour
{
    private enum TargetMode
    {
        Direct,
        RandomGroup
    }

    [Header("Mode")]
    [SerializeField] private TargetMode targetMode = TargetMode.Direct;
    [SerializeField] private PortalTeleport directTarget;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask teleportLayers = ~0;

    [Header("Timing & Motion")]
    [SerializeField] private float preTeleportPause = 0.06f;
    [SerializeField] private float reentryCooldown = 0.12f;
    [SerializeField] private float exitForwardPush = 0.2f;

    [Header("References")]
    [SerializeField] private Transform portalPlane;

    private Collider trigger;
    private readonly Dictionary<Transform, float> nextAllowed = new();

    partial void ResolveAdditionalReferences();
    partial void OnEnablePartial();
    partial void OnDisablePartial();
    partial void UpdatePartial();
    partial void HandleRandomTrigger(Collider other);
    partial void OnValidatePartial();

    private void Reset()
    {
        portalPlane = transform;
    }

    private void OnValidate()
    {
        if (!portalPlane) portalPlane = transform;
        OnValidatePartial();
    }

    private void OnEnable()
    {
        trigger = GetComponent<Collider>();
        if (trigger && !trigger.isTrigger) trigger.isTrigger = true;

        if (!portalPlane) portalPlane = transform;

        ResolveAdditionalReferences();
        OnEnablePartial();
        SyncPortalViewWithCurrentTarget();
    }

    private void OnDisable()
    {
        OnDisablePartial();
    }

    private void Update()
    {
        UpdatePartial();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsCandidate(other)) return;

        if (targetMode == TargetMode.Direct)
        {
            if (!directTarget) return;

            float localZ = portalPlane.InverseTransformPoint(other.bounds.center).z;
            if (localZ < 0f)
            {
                DoTeleport(directTarget, other.transform, other.attachedRigidbody);
            }
        }
        else
        {
            HandleRandomTrigger(other);
        }
    }

    private bool IsCandidate(Collider other)
    {
        if (!other.CompareTag(playerTag)) return false;
        if ((teleportLayers.value & (1 << other.gameObject.layer)) == 0) return false;
        if (nextAllowed.TryGetValue(other.transform, out var t) && Time.time < t) return false;
        return true;
    }

    private void DoTeleport(PortalTeleport destination, Transform target, Rigidbody rb)
    {
        if (!destination) return;

        PerformPreTeleport(target);

        Transform fromPlane = portalPlane ? portalPlane : transform;
        Transform toPlane = destination.portalPlane ? destination.portalPlane : destination.transform;

        Vector3 local = fromPlane.InverseTransformPoint(target.position);
        local = new Vector3(-local.x, local.y, -local.z);

        Vector3 exitWorld = toPlane.TransformPoint(local) + toPlane.forward * exitForwardPush;

        Quaternion delta = toPlane.rotation *
                           Quaternion.Inverse(fromPlane.rotation * Quaternion.Euler(0, 180, 0));

        ApplyTransform(target, rb, exitWorld, delta);

        float until = Time.time + reentryCooldown;
        nextAllowed[target] = until;
        destination.nextAllowed[target] = until;
    }

    private void PerformPreTeleport(Transform target)
    {
        StartCoroutine(TeleportUtils.PausePlayer(target, preTeleportPause));
    }

    private void ApplyTransform(Transform target, Rigidbody rb, Vector3 exitWorld, Quaternion delta)
    {
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = delta * rb.linearVelocity;
#else
            rb.velocity = delta * rb.velocity;
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

public partial class PortalTeleport
{
    [Header("Network / Grouping")]
    [SerializeField] private string group = "Default";
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private bool randomizeEveryOpen = true;
    [SerializeField] private int avoidImmediateRepeat = 1;

    [Header("Optional Components (auto-resolve when empty)")]
    [SerializeField] private Door door;
    [SerializeField] private PortalView portalView;

    [Header("Runtime (read-only)")]
    [SerializeField] private PortalTeleport currentTarget;
    [SerializeField] private List<PortalTeleport> poolPreview = new();

    private bool lastOpen = false;

    private static readonly Dictionary<string, List<PortalTeleport>> registry
        = new Dictionary<string, List<PortalTeleport>>();

    partial void ResolveAdditionalReferences()
    {
        if (targetMode != TargetMode.RandomGroup) return;

        if (!door) door = GetComponentInChildren<Door>(true) ?? GetComponentInParent<Door>();
        if (!portalView) portalView = GetComponentInChildren<PortalView>(true) ?? GetComponentInParent<PortalView>();
    }

    partial void OnEnablePartial()
    {
        if (targetMode == TargetMode.RandomGroup)
        {
            RegisterIntoGroup();

            lastOpen = door ? door.open : false;
            if (randomizeOnStart) PickNewTarget();
        }
        else
        {
            currentTarget = directTarget;
        }
    }

    partial void OnDisablePartial()
    {
        if (targetMode == TargetMode.RandomGroup)
        {
            UnregisterFromGroup();
        }
    }

    partial void UpdatePartial()
    {
        if (targetMode == TargetMode.RandomGroup)
        {
            if (randomizeEveryOpen && door && door.open && !lastOpen)
            {
                PickNewTarget();
            }

            lastOpen = door ? door.open : lastOpen;
        }
        else
        {
            if (currentTarget != directTarget)
            {
                currentTarget = directTarget;
                SyncPortalViewWithCurrentTarget();
            }
        }
    }

    partial void HandleRandomTrigger(Collider other)
    {
        if (targetMode != TargetMode.RandomGroup) return;
        if (!currentTarget) return;

        Transform plane = portalPlane ? portalPlane : transform;
        float localZ = plane.InverseTransformPoint(other.bounds.center).z;

        if (localZ < 0f)
        {
            DoTeleport(currentTarget, other.transform, other.attachedRigidbody);
        }
    }

    partial void OnValidatePartial()
    {
        if (targetMode != TargetMode.RandomGroup)
        {
            currentTarget = directTarget;
        }
    }

    void RegisterIntoGroup()
    {
        if (!registry.TryGetValue(group, out var list))
        {
            list = new List<PortalTeleport>();
            registry[group] = list;
        }

        if (!list.Contains(this)) list.Add(this);
        poolPreview = list;
    }

    void UnregisterFromGroup()
    {
        if (registry.TryGetValue(group, out var list))
        {
            list.Remove(this);
        }
    }

    public void PickNewTarget()
    {
        if (targetMode != TargetMode.RandomGroup) return;
        if (!registry.TryGetValue(group, out var list)) return;

        List<PortalTeleport> candidates = list.FindAll(p => p && p != this);
        if (candidates.Count == 0)
        {
            currentTarget = null;
            SyncPortalViewWithCurrentTarget();
            return;
        }

        PortalTeleport pick = candidates[Random.Range(0, candidates.Count)];
        if (avoidImmediateRepeat > 0 && currentTarget && candidates.Count > 1)
        {
            int tries = 8;
            while (pick == currentTarget && tries-- > 0)
            {
                pick = candidates[Random.Range(0, candidates.Count)];
            }
        }

        currentTarget = pick;
        SyncPortalViewWithCurrentTarget();
    }

    void SyncPortalViewWithCurrentTarget()
    {
        var myPV = portalView ? portalView : GetComponentInChildren<PortalView>(true) ?? GetComponentInParent<PortalView>();
        var targetPV = currentTarget
            ? (currentTarget.portalView ? currentTarget.portalView : currentTarget.GetComponentInChildren<PortalView>(true) ?? currentTarget.GetComponentInParent<PortalView>())
            : null;

        if (myPV)
        {
            myPV.otherPortal = targetPV;
            myPV.ForceRebindRT();
        }
    }
}
