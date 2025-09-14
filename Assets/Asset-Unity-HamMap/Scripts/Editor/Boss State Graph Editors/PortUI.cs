using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public static class PortUI
{
    /// <summary>
    /// Create a styled port (horizontal) with a label + connection-count badge.
    /// </summary>
    public static Port Make(
        Node owner,
        Direction direction,
        Port.Capacity capacity,
        string portName,
        string tooltip,
        Color portColor,
        IEdgeConnectorListener listener)
    {
        // Use Horizontal so lines don't "bounce" up/down first.
        var port = owner.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
        port.portName = portName ?? "";
        port.tooltip  = tooltip ?? "";
        port.portColor = portColor;

        // --- Connection count badge ---
        var badge = new Label("0");
        badge.AddToClassList("port-badge");
        badge.style.unityTextAlign = TextAnchor.MiddleCenter;
        badge.style.fontSize = 10;
        badge.style.minWidth = 14;
        badge.style.height = 14;
        badge.style.marginLeft = 4;
        badge.style.marginRight = 4;
        badge.style.borderTopLeftRadius = 7;
        badge.style.borderTopRightRadius = 7;
        badge.style.borderBottomLeftRadius = 7;
        badge.style.borderBottomRightRadius = 7;
        badge.style.backgroundColor = new Color(0, 0, 0, 0.35f);
        badge.style.color = Color.white;

        // --- Wrap built-in port label + badge into a row we can flip later ---
        var row = new VisualElement { name = "port-row" };
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.flexGrow = 1;

        var label = port.Q<Label>("type"); // GraphView uses name "type" for the port label
        if (label != null)
        {
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(label);
        }

        row.Add(badge);

        // Insert row into port *before* the connector initially; we'll reorder in AlignToParentSide().
        // We don't know the exact connector element yet, so just add the row now; we'll fix order later.
        port.Add(row);

        // Stash badge for UpdateBadge
        port.userData = badge;

        // Edge connector listener (drag behaviour)
        if (listener != null)
            port.AddManipulator(new EdgeConnector<Edge>(listener));

        return port;
    }

    /// <summary>
    /// Update the badge to reflect # of connections and apply a subtle color cue.
    /// </summary>
    public static void UpdateBadge(Port port)
    {
        if (port?.userData is Label badge)
        {
            int count = port.connections?.Count() ?? 0;
            badge.text = count.ToString();
            badge.style.backgroundColor = count > 0
                ? new Color(0.2f, 0.6f, 0.2f, 0.85f) // green when connected
                : new Color(0, 0, 0, 0.35f);         // gray when empty
        }
    }

    /// <summary>
    /// Align the label/badge and physically move the connector to match the side
    /// the port is currently on. Pass the leftContainer so we can auto-detect side.
    /// </summary>
    public static void AlignToParentSide(Port port, VisualElement leftContainer)
    {
        if (port == null || leftContainer == null) return;

        var row   = port.Q<VisualElement>("port-row");
        var label = port.Q<Label>("type");
        if (row == null || label == null) return;

        // Detect which side this port currently lives on by its parent
        bool onLeft = port.parent == leftContainer;

        // 1) Flip label alignment so it points toward the node's center
        row.style.flexDirection = onLeft ? FlexDirection.Row : FlexDirection.RowReverse;

        label.style.flexGrow = 1;
        label.style.unityTextAlign = onLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        label.style.marginLeft  = onLeft ? 0 : 4;
        label.style.marginRight = onLeft ? 4 : 0;

        // Padding so the edge of the node feels balanced
        port.style.paddingLeft  = onLeft ? 6 : 2;
        port.style.paddingRight = onLeft ? 2 : 6;

        // 2) Physically move the connector "dot" to the outside edge.
        //    The connector is another child of the Port; we reorder children so:
        //    - Left side: [connector][row]
        //    - Right side: [row][connector]
        MoveConnectorForSide(port, row, onLeft);
    }

    // ---- Helpers ----

    /// <summary>
    /// Reorder the connector child to the left or right side of the port.
    /// </summary>
    private static void MoveConnectorForSide(Port port, VisualElement row, bool connectorOnLeft)
    {
        // Try to find the connector element by common names/classes, else take the "other" child.
        VisualElement connector =
            port.Q<VisualElement>("connector") ??
            port.Q<VisualElement>("connectorContainer") ??
            port.Children().FirstOrDefault(c => c != row);

        if (connector == null || row == null) return;

        // Remove both, then reinsert in desired order
        connector.RemoveFromHierarchy();
        row.RemoveFromHierarchy();

        if (connectorOnLeft)
        {
            port.Add(connector); // left/outside
            port.Add(row);       // inside/center
        }
        else
        {
            port.Add(row);       // inside/center
            port.Add(connector); // right/outside
        }
    }
}
