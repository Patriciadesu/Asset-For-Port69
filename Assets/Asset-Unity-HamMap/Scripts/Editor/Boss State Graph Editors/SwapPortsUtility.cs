using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public static class SwapPortsUtility
{
    /// <summary>
    /// Swap roles (Input&lt;-&gt;Output) and move them to opposite sides, then reconnect edges
    /// in the most sensible way (Output-&gt;Input).
    /// </summary>
    public static void SwapSidesPersistent(
        Node owner,
        ref Port input, ref Port output,
        VisualElement leftContainer, VisualElement rightContainer,
        ScriptableObject dataAsset,                        // BossStateNode or StateTransition
        System.Action<bool> saveOutputOnLeft              // callback to set bool on asset
    )
    {
        if (input == null || output == null || leftContainer == null || rightContainer == null) return;

        // IMPORTANT: detect current side BEFORE removing from hierarchy
        bool inputWasLeft  = input.parent  == leftContainer;
        bool outputWasLeft = output.parent == leftContainer;

        // Move to the opposite side
        input.RemoveFromHierarchy();
        output.RemoveFromHierarchy();

        if (inputWasLeft)
        {
            rightContainer.Add(input);
            leftContainer.Add(output);
        }
        else
        {
            leftContainer.Add(input);
            rightContainer.Add(output);
        }

        // Re-align both ports based on their new parents
        PortUI.AlignToParentSide(input,  leftContainer);
        PortUI.AlignToParentSide(output, rightContainer);

        // Persist: OUTPUT is now on the opposite of what it was
        bool outputNowLeft = !outputWasLeft;
        if (dataAsset != null && saveOutputOnLeft != null)
        {
            UnityEditor.Undo.RecordObject(dataAsset, "Swap Ports Side");
            saveOutputOnLeft(outputNowLeft);
            UnityEditor.EditorUtility.SetDirty(dataAsset);
        }

        owner.RefreshPorts();
    }
    }
