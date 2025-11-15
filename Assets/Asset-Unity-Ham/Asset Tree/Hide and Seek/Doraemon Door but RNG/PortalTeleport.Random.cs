using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using NaughtyAttributes;

public partial class PortalTeleport
{
    static partial void ConfigureRandomAvailability()
    {
        randomModuleAvailable = true;
    }

    private const string GroupFoldout = "Network / Grouping";
    private const string OptionalFoldout = "Optional Components";
    private const string RuntimeFoldout = "Runtime Preview";

    [Foldout(GroupFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private string group = "Default";
    [Foldout(GroupFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private bool randomizeOnStart = true;
    [Foldout(GroupFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private bool randomizeEveryOpen = true;
    [Foldout(GroupFoldout), ShowIf(nameof(IsRandomMode)), Min(0)]
    [SerializeField] private int avoidImmediateRepeat = 1;

    [Foldout(OptionalFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private MonoBehaviour doorBehaviour;
    [Foldout(OptionalFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private PortalView portalView;

    [Foldout(RuntimeFoldout), ShowIf(nameof(IsRandomMode)), ReadOnly]
    [SerializeField] private PortalTeleport currentTarget;
    [Foldout(RuntimeFoldout), ShowIf(nameof(IsRandomMode))]
    [SerializeField] private List<PortalTeleport> poolPreview = new();

    private bool lastOpen = false;
    private Func<bool> doorOpenGetter;

    private static readonly Dictionary<string, List<PortalTeleport>> registry
        = new Dictionary<string, List<PortalTeleport>>();

    partial void ResolveAdditionalReferences()
    {
        if (!portalView)
        {
            portalView = GetComponentInChildren<PortalView>(true) ?? GetComponentInParent<PortalView>();
        }

        if (targetMode != TargetMode.RandomGroup)
            return;

        if (!doorBehaviour) doorBehaviour = FindDoorBehaviour();
        ConfigureDoorAccessor();
    }

    partial void OnEnablePartial()
    {
        if (targetMode == TargetMode.RandomGroup)
        {
            RegisterIntoGroup();

            lastOpen = GetDoorOpenState();
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
            bool currentOpen = GetDoorOpenState();
            if (randomizeEveryOpen && currentOpen && !lastOpen)
            {
                PickNewTarget();
            }

            lastOpen = currentOpen;
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

        PortalTeleport pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (avoidImmediateRepeat > 0 && currentTarget && candidates.Count > 1)
        {
            int tries = 8;
            while (pick == currentTarget && tries-- > 0)
            {
                pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
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

    private MonoBehaviour FindDoorBehaviour()
    {
        MonoBehaviour result = null;
        result = GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(IsDoorLikeComponent);
        if (result) return result;
        result = GetComponentsInParent<MonoBehaviour>(true)
            .FirstOrDefault(IsDoorLikeComponent);
        return result;
    }

    private bool IsDoorLikeComponent(MonoBehaviour component)
    {
        if (!component) return false;
        var type = component.GetType();
        string name = type.Name;
        string full = type.FullName ?? name;
        return string.Equals(name, "Door", StringComparison.OrdinalIgnoreCase)
               || full.Contains(".Door", StringComparison.OrdinalIgnoreCase);
    }

    private void ConfigureDoorAccessor()
    {
        doorOpenGetter = CreateDoorOpenGetter(doorBehaviour);
    }

    private Func<bool> CreateDoorOpenGetter(Component component)
    {
        if (!component) return null;
        var type = component.GetType();

        var prop = type.GetProperty("open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return () => component ? (bool)prop.GetValue(component) : false;
        }

        var field = type.GetField("open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            return () => component ? (bool)field.GetValue(component) : false;
        }

        var method = type.GetMethod("IsOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (method != null && method.ReturnType == typeof(bool))
        {
            return () => component ? (bool)method.Invoke(component, null) : false;
        }

        return null;
    }

    private bool GetDoorOpenState()
    {
        if (doorOpenGetter == null && doorBehaviour)
        {
            ConfigureDoorAccessor();
        }
        return doorOpenGetter?.Invoke() ?? false;
    }
}

