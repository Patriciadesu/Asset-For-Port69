using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class VerticalEdge : Edge
{
    public VerticalEdge()
    {
        capabilities |= Capabilities.Selectable | Capabilities.Deletable;
        pickingMode = PickingMode.Position;

        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", _ =>
            {
                var gv = this.GetFirstAncestorOfType<GraphView>();
                if (gv == null) return;
                gv.RemoveElement(this);
            });
        }));
    }

    public override bool UpdateEdgeControl()
    {
        if (edgeControl == null) return false;

        Vector2 fromWorld = output != null ? output.GetGlobalCenter() : Vector2.zero;
        Vector2 toWorld   = input  != null ? input.GetGlobalCenter()  : Vector2.zero;

        Vector2 from = edgeControl.WorldToLocal(fromWorld);
        Vector2 to   = edgeControl.WorldToLocal(toWorld);

        // Horizontal avoids up/down “bounce”
        edgeControl.outputOrientation = Orientation.Horizontal;
        edgeControl.inputOrientation  = Orientation.Horizontal;
        edgeControl.from = from;
        edgeControl.to   = to;

        edgeControl.MarkDirtyRepaint();
        return true;
    }
}
