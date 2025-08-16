using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BossStateNodeView : Node
{
    public Port input;   // exposed for graph wiring
    public Port output;  // exposed for graph wiring
    public BossStateNode nodeData;
    private readonly System.Action<BossStateNodeView> onSelected;

    public BossStateNodeView(BossStateNode nodeData,
                             System.Action<BossStateNodeView> onSelected = null,
                             IEdgeConnectorListener edgeListener = null)
    {
        this.nodeData = nodeData;
        this.onSelected = onSelected;

        title = nodeData.stateName;
        style.width = 240;

        // Top input / bottom output (vertical flow)
        input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "";
        titleContainer.Add(input);

        output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "";
        extensionContainer.Add(output);

        // Use custom edge creation
        if (edgeListener != null)
        {
            // IMPORTANT: EdgeConnector generic should be Edge
            input.AddManipulator(new EdgeConnector<Edge>(edgeListener));
            output.AddManipulator(new EdgeConnector<Edge>(edgeListener));
        }

        // Editable fields
        var nameField = new TextField("Name") { value = nodeData.stateName };
        nameField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(nodeData, "Edit State Name");
            nodeData.stateName = evt.newValue;
            title = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(nameField);

        var initToggle = new Toggle("Initial") { value = nodeData.isInitialState };
        initToggle.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(nodeData, "Toggle Initial");
            nodeData.isInitialState = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(initToggle);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(Random.Range(50, 400), Random.Range(50, 400), 240, 160));

        // Right-click → Delete node
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", _ => this.GetFirstAncestorOfType<GraphView>()?.DeleteSelection());
        }));
        this.AddManipulator(new ContextualMenuManipulator(evt =>
{
    var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;

    evt.menu.AppendAction("Disconnect", _ =>
    {
        gv?.DisconnectStateNode(this);
    });

    evt.menu.AppendSeparator();

    evt.menu.AppendAction("Delete", _ =>
    {
        gv?.DeleteSelection(); // deletes node; your GraphViewChange handles cleanup
    });
}));
    }

    public override void OnSelected()
    {
        base.OnSelected();
        onSelected?.Invoke(this);
    }

    public void RefreshNodeFromData()
    {
        title = nodeData != null ? nodeData.stateName : "State";
        RefreshExpandedState();
        RefreshPorts();
    }
}
