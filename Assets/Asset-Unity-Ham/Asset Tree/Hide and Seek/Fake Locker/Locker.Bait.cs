using System.Collections;
using NaughtyAttributes;
using UnityEngine;

public partial class Locker
{
    [Header("Bait Mode")]
    [ShowIf(nameof(IsBaitMode))]
    [SerializeField] private float timeBeforeDie = 2f;

    private Player playerScript;
    private Coroutine baitCoroutine;

    partial void OnStartExtra()
    {
        if (!IsBaitMode) return;
        ResolvePlayerScript();
    }

    partial void OnUpdateExtra() { }

    partial void OnBeforeEnterExtra()
    {
        if (!IsBaitMode) return;

        ResolvePlayerScript();

        if (baitCoroutine != null)
        {
            StopCoroutine(baitCoroutine);
        }

        baitCoroutine = StartCoroutine(BaitCountdown());
    }

    partial void OnAfterEnterExtra() { }

    partial void OnAfterExitExtra()
    {
        if (!IsBaitMode) return;

        if (baitCoroutine != null)
        {
            StopCoroutine(baitCoroutine);
            baitCoroutine = null;
        }
    }

    partial void OnDisableExtra()
    {
        if (!IsBaitMode) return;

        if (baitCoroutine != null)
        {
            StopCoroutine(baitCoroutine);
            baitCoroutine = null;
        }
    }

    private IEnumerator BaitCountdown()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, timeBeforeDie));

        if (IsBaitMode && IsHiding && playerScript != null && playerScript.Stat != null)
        {
            playerScript.Stat.currenthealth = 0;
        }

        baitCoroutine = null;
    }

    private void ResolvePlayerScript()
    {
        EnsurePlayerReference();
        if (!playerScript && player)
        {
            playerScript = player.GetComponent<Player>();
        }
    }

    static partial void AppendAdditionalModes(DropdownList<LockerMode> modes)
    {
        modes.Add("Bait", LockerMode.Bait);
    }

    static partial void CheckModeSupport(LockerMode candidate, ref bool supported)
    {
        if (candidate == LockerMode.Bait)
        {
            supported = true;
        }
    }
}

