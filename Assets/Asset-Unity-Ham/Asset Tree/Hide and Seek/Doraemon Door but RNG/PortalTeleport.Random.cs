using System.Collections.Generic;
using UnityEngine;
using DoorScript;
using NaughtyAttributes;

public partial class PortalTeleport
{
    static partial void ConfigureRandomAvailability()
    {
        randomModuleAvailable = true;
    }

    [Header("Network / Grouping")]
    [ShowIf(nameof(IsRandomMode))]
    [HideInInspector] [SerializeField] private string group = "Default";
    [ShowIf(nameof(IsRandomMode))]
    [HideInInspector] [SerializeField] private bool randomizeOnStart = true;
    [ShowIf(nameof(IsRandomMode))]
    [SerializeField] private bool randomizeEveryOpen = true;
    [ShowIf(nameof(IsRandomMode))]
    [Min(0)]
    [HideInInspector] [SerializeField] private int avoidImmediateRepeat = 1;

    [Header("Optional Components (auto-resolve when empty)")]
    [ShowIf(nameof(IsRandomMode))]
    [HideInInspector] [SerializeField] private Door door;
    [ShowIf(nameof(IsRandomMode))]
    [HideInInspector] [SerializeField] private PortalView portalView;

    [Header("Runtime (read-only)")]
    [ShowIf(nameof(IsRandomMode))]
    [ReadOnly]
    [SerializeField] private PortalTeleport currentTarget;
    [HideInInspector]
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

    partial void SyncPortalViewWithCurrentTarget()
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

