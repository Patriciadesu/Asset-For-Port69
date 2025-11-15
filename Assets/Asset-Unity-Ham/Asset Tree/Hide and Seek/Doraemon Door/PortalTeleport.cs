using System.Collections;
using System.Collections.Generic;
using DoorScript;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Collider))]
public partial class PortalTeleport : MonoBehaviour
{
    private enum TargetMode
    {
        Direct,
        RandomGroup
    }

    private const string FilterFoldout = "Filter";
    private const string TimingFoldout = "Timing & Motion";
    private const string TriggerFoldout = "Trigger Volume";
    private const string ReferencesFoldout = "References";

    [Header("Mode")]
    [Dropdown(nameof(GetAvailableModes))]
    [SerializeField] private TargetMode targetMode = TargetMode.Direct;
    [ShowIf(nameof(IsDirectMode))]
    [SerializeField] private PortalTeleport directTarget;

    [Foldout(FilterFoldout)]
    [SerializeField] private string playerTag = "Player";
    [Foldout(FilterFoldout)]
    [SerializeField] private LayerMask teleportLayers = ~0;

    [Foldout(TimingFoldout)]
    [SerializeField] private float preTeleportPause = 0.06f;
    [Foldout(TimingFoldout)]
    [SerializeField] private float reentryCooldown = 0.12f;
    [Foldout(TimingFoldout)]
    [SerializeField] private float exitForwardPush = 0.2f;

    [Foldout(TriggerFoldout)]
    [SerializeField, Min(0.05f)] private float triggerDepth = 0.65f;
    [Foldout(TriggerFoldout)]
    [SerializeField, Min(0.05f)] private float triggerWidth = 1.5f;
    [Foldout(TriggerFoldout)]
    [SerializeField, Min(0.05f)] private float triggerHeight = 2.2f;

    [Foldout(ReferencesFoldout)]
    [SerializeField] private Transform portalPlane;
    [Foldout(ReferencesFoldout)]
    [SerializeField] private Door linkedDoor;

    [Header("Arrival")]
    [SerializeField] private Transform arrivalAnchor;
    [SerializeField] private float arrivalOffset = 0.75f;
    [SerializeField, Tooltip("Blend player motion over a short duration for smoother exits (non-Rigidbody targets).")]
    private bool smoothArrival = true;
    [SerializeField, Range(0.01f, 1f)]
    private float smoothArrivalDuration = 0.15f;
    [SerializeField]
    private AnimationCurve arrivalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private const string ArrivalAnchorName = "ArrivalAnchor";
    private const float DefaultAnchorForwardOffset = 0.85f;
    private readonly Dictionary<Transform, Coroutine> activeSmoothTransitions = new();

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
        EnsureArrivalAnchor();
        EnsureDoorReference();
    }

    private void OnValidate()
    {
        if (!portalPlane) portalPlane = transform;
        if (arrivalOffset < 0f) arrivalOffset = 0f;
        EnsureDoorReference();
        ConfigureTriggerVolume();
        EnsureArrivalAnchor();
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
        ConfigureTriggerVolume();

        if (!portalPlane) portalPlane = transform;
        EnsureArrivalAnchor();
        EnsureDoorReference();

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
        foreach (var kvp in activeSmoothTransitions)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeSmoothTransitions.Clear();
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

        destination.ApplyTransform(target, rb, exitWorld, delta);

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
            if (smoothArrival && smoothArrivalDuration > 0f)
            {
                if (activeSmoothTransitions.TryGetValue(target, out var running) && running != null)
                {
                    StopCoroutine(running);
                }
                var routine = StartCoroutine(SmoothArrivalCoroutine(target, exitWorld, delta * target.rotation));
                activeSmoothTransitions[target] = routine;
            }
            else
            {
                target.SetPositionAndRotation(exitWorld, delta * target.rotation);
            }
        }
    }

    private IEnumerator SmoothArrivalCoroutine(Transform target, Vector3 finalPosition, Quaternion finalRotation)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float elapsed = 0f;
        while (elapsed < smoothArrivalDuration)
        {
            float t = smoothArrivalDuration > 0f ? elapsed / smoothArrivalDuration : 1f;
            float eased = arrivalEase != null ? arrivalEase.Evaluate(t) : t;
            target.SetPositionAndRotation(
                Vector3.Lerp(startPos, finalPosition, eased),
                Quaternion.Slerp(startRot, finalRotation, eased));
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.SetPositionAndRotation(finalPosition, finalRotation);
        activeSmoothTransitions.Remove(target);
    }

    private void EnsureArrivalAnchor()
    {
        if (arrivalAnchor) return;
        Transform found = transform.Find(ArrivalAnchorName);
        if (found)
        {
            arrivalAnchor = found;
            return;
        }

        var anchorGO = new GameObject(ArrivalAnchorName);
        anchorGO.transform.SetParent(transform, false);
        anchorGO.transform.localPosition = Vector3.forward * DefaultAnchorForwardOffset;
        anchorGO.transform.localRotation = Quaternion.identity;
        arrivalAnchor = anchorGO.transform;
    }

    private void EnsureDoorReference()
    {
        if (linkedDoor) return;
        linkedDoor = GetComponentInChildren<Door>(true) ?? GetComponentInParent<Door>();
    }

    private void ConfigureTriggerVolume()
    {
        if (!(trigger is BoxCollider box)) return;

        Vector3 size = box.size;
        if (size == Vector3.zero)
        {
            size = new Vector3(triggerWidth, triggerHeight, triggerDepth);
        }
        else
        {
            size.x = Mathf.Max(triggerWidth, size.x);
            size.y = Mathf.Max(triggerHeight, size.y);
            size.z = Mathf.Max(triggerDepth, size.z);
        }
        box.size = size;

        Vector3 center = box.center;
        center.z = 0f;
        box.center = center;
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

    public Transform ArrivalAnchor => arrivalAnchor;
    public Door AssociatedDoor => linkedDoor;
}
