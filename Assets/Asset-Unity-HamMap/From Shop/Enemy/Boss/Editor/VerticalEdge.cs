using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class VerticalEdge : Edge
{
    public VerticalEdge()
    {
        // Selectable & deletable
        capabilities |= Capabilities.Selectable | Capabilities.Deletable;
        pickingMode = PickingMode.Position;

        // Right-click → Delete this edge
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", _ =>
            {
                var gv = this.GetFirstAncestorOfType<GraphView>();
                if (gv == null) return;
                gv.RemoveElement(this); // triggers elementsToRemove in GraphViewChange
            });
        }));
    }

    public override bool UpdateEdgeControl()
    {
        if (edgeControl == null) return false;

        // Global positions of port centers
        Vector2 fromWorld = output != null ? output.GetGlobalCenter() : Vector2.zero;
        Vector2 toWorld = input != null ? input.GetGlobalCenter() : Vector2.zero;

        // Convert to edgeControl local space (so pan/zoom doesn't break the wire)
        Vector2 from = edgeControl.WorldToLocal(fromWorld);
        Vector2 to = edgeControl.WorldToLocal(toWorld);

        edgeControl.outputOrientation = Orientation.Vertical;
        edgeControl.inputOrientation = Orientation.Vertical;
        edgeControl.from = from;
        edgeControl.to = to;

        float midY = (from.y + to.y) * 0.5f;

        // If your GraphView exposes controlPoints (Unity 6): assign polyline for VFX-like vertical look
        // If your local API doesn't have controlPoints, comment the next line and rely on default beziers.
        edgeControl.outputOrientation = Orientation.Vertical;
        edgeControl.inputOrientation = Orientation.Vertical;
        edgeControl.from = from; edgeControl.to = to;
        edgeControl.MarkDirtyRepaint();
        return true;
    }
}
