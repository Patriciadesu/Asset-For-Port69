using System.Collections.Generic;
using UnityEngine;
using DoorScript;
using NaughtyAttributes;

[RequireComponent(typeof(Collider))]
public partial class PortalTeleport : MonoBehaviour
{
    private enum TargetMode
    {
        Direct,
        RandomGroup
    }

    [Header("Mode")]
    [Dropdown(nameof(GetAvailableModes))]
    [SerializeField] private TargetMode targetMode = TargetMode.Direct;
    [ShowIf(nameof(IsDirectMode))]
    [SerializeField] private PortalTeleport directTarget;

    [Header("Filter")]
    [HideInInspector] [SerializeField] private string playerTag = "Player";
    [HideInInspector] [SerializeField] private LayerMask teleportLayers = ~0;

    [Header("Timing & Motion")]
    [HideInInspector] [SerializeField] private float preTeleportPause = 0.06f;
    [HideInInspector] [SerializeField] private float reentryCooldown = 0.12f;
    [HideInInspector] [SerializeField] private float exitForwardPush = 0.2f;

    [Header("References")]
    [HideInInspector] [SerializeField] private Transform portalPlane;

    [Header("Arrival")]
    [SerializeField] private Transform arrivalAnchor;
    [SerializeField] private float arrivalOffset = 0.75f;

    private static bool randomModuleAvailable;

    static PortalTeleport()
    {
        randomModuleAvailable = false;
        ConfigureRandomAvailability();
    }

    private Collider trigger;
    private readonly Dictionary<Transform, float> nextAllowed = new();

    partial void ResolveAdditionalReferences();
    partial void OnEnablePartial();
    partial void OnDisablePartial();
    partial void UpdatePartial();
    partial void HandleRandomTrigger(Collider other);
    partial void OnValidatePartial();
    static partial void ConfigureRandomAvailability();
    partial void SyncPortalViewWithCurrentTarget();

    private void Reset()
    {
        portalPlane = transform;
    }

    private void OnValidate()
    {
        if (!portalPlane) portalPlane = transform;
        if (arrivalOffset < 0f) arrivalOffset = 0f;
        OnValidatePartial();
        if (!randomModuleAvailable && targetMode == TargetMode.RandomGroup)
        {
            targetMode = TargetMode.Direct;
        }
    }

    private void OnEnable()
    {
        trigger = GetComponent<Collider>();
        if (trigger && !trigger.isTrigger) trigger.isTrigger = true;

        if (!portalPlane) portalPlane = transform;

        if (!randomModuleAvailable && targetMode == TargetMode.RandomGroup)
        {
            targetMode = TargetMode.Direct;
        }

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

        Vector3 exitWorld = toPlane.TransformPoint(local);

        Quaternion delta = toPlane.rotation *
                           Quaternion.Inverse(fromPlane.rotation * Quaternion.Euler(0, 180, 0));

        if (destination.arrivalAnchor)
        {
            var anchor = destination.arrivalAnchor;
            Vector3 anchorLocal = toPlane.InverseTransformPoint(exitWorld);
            exitWorld = anchor.TransformPoint(anchorLocal);

            Quaternion anchorAdjust = anchor.rotation * Quaternion.Inverse(toPlane.rotation);
            delta = anchorAdjust * delta;
        }
        else
        {
            float push = Mathf.Max(destination.arrivalOffset, 0f) + Mathf.Max(destination.exitForwardPush, 0f);
            exitWorld += toPlane.forward * push;
        }

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

    private bool IsDirectMode() => targetMode == TargetMode.Direct;
    private bool IsRandomMode() => randomModuleAvailable && targetMode == TargetMode.RandomGroup;

    private DropdownList<TargetMode> GetAvailableModes()
    {
        var modes = new DropdownList<TargetMode>
        {
            { "Direct Pair", TargetMode.Direct }
        };

        if (randomModuleAvailable)
        {
            modes.Add("Random Group", TargetMode.RandomGroup);
        }

        return modes;
    }
}
