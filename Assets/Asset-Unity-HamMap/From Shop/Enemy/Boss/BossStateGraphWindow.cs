using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class BossStateGraphWindow : EditorWindow
{
    private BossStateGraph selectedGraph;
    private StateMachineGraphView graphView;

    [MenuItem("Window/State Machine Graph")]
    public static void OpenWindow()
    {
        var window = GetWindow<BossStateGraphWindow>();
        window.titleContent = new GUIContent("State Machine Graph");
        window.Show();
    }

    private void OnEnable()
    {
        ConstructGraphView();
        Selection.selectionChanged += OnSelectionChange;
        OnSelectionChange();
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
        Selection.selectionChanged -= OnSelectionChange;
    }

    private void ConstructGraphView()
    {
        graphView = new StateMachineGraphView
        {
            name = "State Machine Graph"
        };
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is BossStateGraph bossGraph)
        {
            selectedGraph = bossGraph;
            titleContent.text = $"State Machine Graph - {bossGraph.name}";
            graphView.PopulateView(selectedGraph);
        }
        else
        {
            selectedGraph = null;
            titleContent.text = "State Machine Graph";
            graphView.ClearGraph();
        }

        Repaint();
    }
}

// =========================
// GraphView Class
// =========================
public class StateMachineGraphView : GraphView
{
    public StateMachineGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public void PopulateView(BossStateGraph graph)
    {
        ClearGraph();

        if (graph == null || graph.stateNodes == null) return;

        foreach (var node in graph.stateNodes)
        {
            AddElement(new BossStateNodeView(node));
        }
    }

    public void ClearGraph()
    {
        graphElements.ForEach(RemoveElement);
    }
}

// =========================
// Editable Styled Node View
// =========================
public class BossStateNodeView : Node
{
    private BossStateNode nodeData;

    public BossStateNodeView(BossStateNode nodeData)
    {
        this.nodeData = nodeData;
        title = nodeData.stateName;
        style.width = 250;
        style.height = 180;

        // Styling: make it look a bit like VFX Graph
        style.borderTopLeftRadius = 6;
        style.borderTopRightRadius = 6;
        style.borderBottomLeftRadius = 6;
        style.borderBottomRightRadius = 6;
        style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

        // Editable state name
        var nameField = new TextField("State Name")
        {
            value = nodeData.stateName
        };
        nameField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(nodeData, "Edit State Name");
            nodeData.stateName = evt.newValue;
            title = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(nameField);

        // Editable initial state toggle
        var initToggle = new Toggle("Initial State")
        {
            value = nodeData.isInitialState
        };
        initToggle.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(nodeData, "Toggle Initial State");
            nodeData.isInitialState = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(initToggle);

        // State Name
var stateNameField = new TextField("State Name (in BossState)")
{
    value = nodeData.state != null ? nodeData.state.stateName : ""
};
stateNameField.RegisterValueChangedCallback(evt =>
{
    if (nodeData.state != null)
    {
        Undo.RecordObject(nodeData, "Edit BossState Name");
        nodeData.state.stateName = evt.newValue;
        EditorUtility.SetDirty(nodeData);
    }
});
mainContainer.Add(stateNameField);

// Stage Enum
var stageField = new EnumField("Stage",
    nodeData.state != null ? nodeData.state.stage : StateStage.Enter);
stageField.RegisterValueChangedCallback(evt =>
{
    if (nodeData.state != null)
    {
        Undo.RecordObject(nodeData, "Edit BossState Stage");
        nodeData.state.stage = (StateStage)evt.newValue;
        EditorUtility.SetDirty(nodeData);
    }
});
mainContainer.Add(stageField);


        // Transitions array size display
        var transitionsLabel = new Label($"Transitions: {(nodeData.transitions != null ? nodeData.transitions.Length : 0)}");
        mainContainer.Add(transitionsLabel);

        // Random position for now
        SetPosition(new Rect(
            Random.Range(100, 400),
            Random.Range(100, 400),
            250,
            180
        ));
    }
}
