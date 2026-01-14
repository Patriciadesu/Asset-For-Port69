using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public partial class PortalTeleport : MonoBehaviour
{
    private enum TargetMode
    {
        Direct,
        Random
    }

    private const string ModeFoldout = "Mode";
    private const string FilterFoldout = "Filter";
    private const string TimingFoldout = "Timing & Motion";
    private const string ReferencesFoldout = "References";

    [Dropdown(nameof(GetAvailableModes))]
    [SerializeField] private TargetMode targetMode = TargetMode.Direct;
    [ShowIf(nameof(IsDirectMode))]
    [SerializeField] private PortalTeleport directTarget;

    [Foldout(FilterFoldout)]
    [SerializeField] private string playerTag = "Player";
    [Foldout(FilterFoldout)]
    [SerializeField] private LayerMask teleportLayers = ~0;

    [Foldout(TimingFoldout)]
    [SerializeField, Range(0.01f, 0.5f)] private float preTeleportPause = 0.05f;
    [Foldout(TimingFoldout)]
    [SerializeField, Range(0.01f, 1f)] private float reentryCooldown = 0.12f;
    [Foldout(TimingFoldout)]
    [SerializeField, Range(0f, 2f)] private float exitForwardPush = 0.2f;

    [Foldout(ReferencesFoldout)]
    [SerializeField] private Transform portalPlane;

    private Collider trigger;
    private readonly Dictionary<Transform, float> nextAllowed = new();

    private static bool randomModuleAvailable;

    static PortalTeleport()
    {
        randomModuleAvailable = false;
        ConfigureRandomAvailability();
    }

    static partial void ConfigureRandomAvailability();

    partial void ResolveAdditionalReferences();
    partial void OnEnablePartial();
    partial void OnDisablePartial();
    partial void UpdatePartial();
    partial void OnValidatePartial();
    partial void HandleRandomTrigger(Collider other);
    partial void SyncPortalViewWithCurrentTarget();

    private void Reset()
    {
        portalPlane = transform;
    }

    private void OnValidate()
    {
        if (!portalPlane) portalPlane = transform;
        EnsureModeValidity();
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

        if (IsDirectMode())
        {
            if (!directTarget) return;

            Transform plane = portalPlane ? portalPlane : transform;
            float localZ = plane.InverseTransformPoint(other.bounds.center).z;
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

        StartCoroutine(TeleportUtils.PausePlayer(target, preTeleportPause));

        Transform fromPlane = portalPlane ? portalPlane : transform;
        Transform toPlane = destination.portalPlane ? destination.portalPlane : destination.transform;

        Vector3 local = fromPlane.InverseTransformPoint(target.position);
        local = new Vector3(-local.x, local.y, -local.z);

        Vector3 exitWorld = toPlane.TransformPoint(local) + toPlane.forward * Mathf.Max(destination.exitForwardPush, 0f);

        Quaternion delta = toPlane.rotation *
                           Quaternion.Inverse(fromPlane.rotation * Quaternion.Euler(0, 180, 0));

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

        PortalFacingUtils.AlignPlayerFacingOut(target, toPlane);

        float until = Time.time + reentryCooldown;
        nextAllowed[target] = until;
        destination.nextAllowed[target] = until;
    }

    private DropdownList<TargetMode> GetAvailableModes()
    {
        var modes = new DropdownList<TargetMode>
        {
            { "Direct", TargetMode.Direct }
        };

        if (randomModuleAvailable)
        {
            modes.Add("Random", TargetMode.Random);
        }

        return modes;
    }

    private void EnsureModeValidity()
    {
        if (!randomModuleAvailable && targetMode == TargetMode.Random)
        {
            targetMode = TargetMode.Direct;
        }
    }

    private bool IsDirectMode() => targetMode == TargetMode.Direct;
    private bool IsRandomMode() => randomModuleAvailable && targetMode == TargetMode.Random;
}

