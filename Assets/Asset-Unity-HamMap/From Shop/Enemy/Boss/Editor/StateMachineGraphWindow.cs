using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class StateMachineGraphWindow : EditorWindow
{
    private BossStateGraph selectedGraph;
    private StateMachineGraphView graphView;
    private UnityEditor.UIElements.Toolbar toolbar; // If inaccessible, replace with VisualElement (see comment below)

    [MenuItem("Window/State Machine Graph")]
    public static void OpenWindow()
    {
        var window = GetWindow<StateMachineGraphWindow>();
        window.titleContent = new GUIContent("State Machine Graph");
        window.Show();
    }

    private void OnEnable()
    {
        ConstructGraphView();
        ConstructToolbar();

        Selection.selectionChanged += OnSelectionChange;
        OnSelectionChange();
    }

    private void OnDisable()
    {
        if (graphView != null) rootVisualElement.Remove(graphView);
        if (toolbar != null) rootVisualElement.Remove(toolbar);
        Selection.selectionChanged -= OnSelectionChange;
    }

    private void ConstructGraphView()
    {
        graphView = new StateMachineGraphView(
            requestNewState: CreateNewStateNodeAsset,
            requestNewTransition: CreateNewTransitionAsset
        )
        { name = "State Machine Graph" };

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void ConstructToolbar()
    {
        // If this type is inaccessible in your install, replace next line with:
        // var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        toolbar = new UnityEditor.UIElements.Toolbar();

        var btnNewState = new UnityEditor.UIElements.ToolbarButton(() =>
        {
            if (!EnsureGraphSelected()) return;
            var created = CreateNewStateNodeAsset("New State");
            if (created != null) graphView.AddOrUpdateStateNodeView(created);
        })
        { text = "+ State" };

        var btnNewTransition = new UnityEditor.UIElements.ToolbarButton(() =>
        {
            if (!EnsureGraphSelected()) return;
            BossStateNode attachTo = graphView.GetCurrentSelectedStateNode();
            var created = CreateNewTransitionAsset("New Transition", attachTo);
            if (created != null) graphView.AddOrUpdateTransitionNodeView(created);
        })
        { text = "+ Transition" };

        toolbar.Add(btnNewState);
        toolbar.Add(btnNewTransition);
        rootVisualElement.Add(toolbar);
    }

    private bool EnsureGraphSelected()
    {
        if (selectedGraph == null)
        {
            ShowNotification(new GUIContent("Select a BossStateGraph in the Project window first."));
            return false;
        }
        return true;
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

    // -------- Asset Create Helpers --------
    private string GetGraphFolderPath()
    {
        if (selectedGraph == null) return "Assets";
        var graphPath = AssetDatabase.GetAssetPath(selectedGraph);
        return string.IsNullOrEmpty(graphPath) ? "Assets" : Path.GetDirectoryName(graphPath).Replace("\\", "/");
    }

    private BossStateNode CreateNewStateNodeAsset(string baseName)
    {
        if (selectedGraph == null) return null;

        var folder = GetGraphFolderPath();
        var asset = ScriptableObject.CreateInstance<BossStateNode>();
        asset.stateName = baseName;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        var list = new List<BossStateNode>(selectedGraph.stateNodes ?? new BossStateNode[0]);
        Undo.RecordObject(selectedGraph, "Add State Node");
        list.Add(asset);
        selectedGraph.stateNodes = list.ToArray();
        EditorUtility.SetDirty(selectedGraph);
        AssetDatabase.SaveAssets();

        return asset;
    }

    private StateTransition CreateNewTransitionAsset(string baseName, BossStateNode attachToState = null)
{
    if (selectedGraph == null) return null;

    var folder = GetGraphFolderPath();
    var asset = ScriptableObject.CreateInstance<StateTransition>();
    string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
    AssetDatabase.CreateAsset(asset, path);
    AssetDatabase.SaveAssets();

    // NEW: add to graph.transitionNodes list
    {
        Undo.RecordObject(selectedGraph, "Add Transition Node");
        var tlist = new List<StateTransition>(selectedGraph.transitionNodes ?? new StateTransition[0]);
        tlist.Add(asset);
        selectedGraph.transitionNodes = tlist.ToArray();
        EditorUtility.SetDirty(selectedGraph);
        AssetDatabase.SaveAssets();
    }

    // Optional: also attach to currently selected state (as before)
    if (attachToState != null)
    {
        Undo.RecordObject(attachToState, "Attach Transition");
        var list = new List<StateTransition>(attachToState.transitions ?? new StateTransition[0]);
        list.Add(asset);
        attachToState.transitions = list.ToArray();
        EditorUtility.SetDirty(attachToState);
        AssetDatabase.SaveAssets();
    }

    return asset;
}

}
