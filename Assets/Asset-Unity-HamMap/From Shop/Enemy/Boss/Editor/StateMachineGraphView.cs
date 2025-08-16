using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class StateMachineGraphView : GraphView
{
    private readonly System.Func<string, BossStateNode> requestNewState;
    private readonly System.Func<string, BossStateNode, StateTransition> requestNewTransition;

    private readonly Dictionary<BossStateNode, BossStateNodeView> stateNodeViews = new();
    private readonly Dictionary<StateTransition, StateTransitionNodeView> transitionNodeViews = new();

    internal readonly VerticalEdgeConnectorListener edgeListener;

    private BossStateGraph currentGraph;
    private BossStateNodeView lastSelectedStateView;

    public BossStateNode GetCurrentSelectedStateNode()
        => lastSelectedStateView != null ? lastSelectedStateView.nodeData : null;

    internal void NotifyStateSelected(BossStateNodeView view)
    {
        lastSelectedStateView = view;
    }

    public StateMachineGraphView(
        System.Func<string, BossStateNode> requestNewState,
        System.Func<string, BossStateNode, StateTransition> requestNewTransition)
    {
        this.requestNewState = requestNewState;
        this.requestNewTransition = requestNewTransition;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Repaint edges when panning/zooming so they stay glued to ports
        viewTransformChanged += (gv) =>
        {
            foreach (var e in graphElements.ToList())
                if (e is Edge ed) ed.MarkDirtyRepaint();
        };

        graphViewChanged = OnGraphViewChanged;
        edgeListener = new VerticalEdgeConnectorListener(this);
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;

            
        }));
    }

    public void PopulateView(BossStateGraph graph)
{
    ClearGraph();
    currentGraph = graph;
    stateNodeViews.Clear();
    transitionNodeViews.Clear();

    if (graph == null) return;

    // ---- 1) States ----
    if (graph.stateNodes != null)
    {
        foreach (var node in graph.stateNodes.Where(n => n != null))
            AddOrUpdateStateNodeView(node);
    }

    // ---- 2) Transitions (show ALL) ----
    if (graph.transitionNodes != null)
    {
        foreach (var t in graph.transitionNodes.Where(t => t != null))
            AddOrUpdateTransitionNodeView(t);
    }

    // ---- 3) Edges from State->Transition (based on state's transitions[]) ----
    if (graph.stateNodes != null)
    {
        foreach (var state in graph.stateNodes.Where(n => n != null))
        {
            if (state.transitions == null) continue;
            foreach (var t in state.transitions.Where(t => t != null))
            {
                // Ensure the transition view exists (in case it's not listed yet)
                AddOrUpdateTransitionNodeView(t);

                var e1 = new VerticalEdge
                {
                    output = stateNodeViews[state].output,
                    input  = transitionNodeViews[t].input
                };
                e1.output.Connect(e1);
                e1.input.Connect(e1);
                AddElement(e1);
            }
        }
    }

    // ---- 4) Edges from Transition->State (based on transition's nextStates[]) ----
    if (graph.transitionNodes != null)
    {
        foreach (var t in graph.transitionNodes.Where(t => t != null))
        {
            if (t.nextStates == null) continue;
            foreach (var next in t.nextStates.Where(ns => ns != null))
            {
                // Ensure the state view exists (in case graph/stateNodes got out-of-sync)
                AddOrUpdateStateNodeView(next);

                var e2 = new VerticalEdge
                {
                    output = transitionNodeViews[t].output,
                    input  = stateNodeViews[next].input
                };
                e2.output.Connect(e2);
                e2.input.Connect(e2);
                AddElement(e2);
            }
        }
    }
}

    public void ClearGraph()
    {
        var toRemove = graphElements.ToList();
        foreach (var el in toRemove) RemoveElement(el);
    }

    public void AddOrUpdateStateNodeView(BossStateNode node)
    {
        if (!stateNodeViews.TryGetValue(node, out var view))
        {
            view = new BossStateNodeView(node, NotifyStateSelected, edgeListener);
            AddElement(view);
            stateNodeViews[node] = view;
        }
        else
        {
            view.RefreshNodeFromData();
        }
    }

    public void AddOrUpdateTransitionNodeView(StateTransition t)
    {
        if (!transitionNodeViews.TryGetValue(t, out var view))
        {
            view = new StateTransitionNodeView(t, edgeListener);
            AddElement(view);
            transitionNodeViews[t] = view;
        }
        else
        {
            view.RefreshNodeFromData();
        }
    }

    // Only allow State(out)->Transition(in) and Transition(out)->State(in)
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var result = new List<Port>();

        foreach (var candidate in ports.ToList())
        {
            if (candidate == startPort) continue;
            if (candidate.direction == startPort.direction) continue;
            if (candidate.node == startPort.node) continue;
            if (candidate.orientation != startPort.orientation) continue;

            bool startIsState = startPort.node is BossStateNodeView;
            bool startIsTransition = startPort.node is StateTransitionNodeView;
            bool candIsState = candidate.node is BossStateNodeView;
            bool candIsTransition = candidate.node is StateTransitionNodeView;

            bool valid =
                (startIsState && startPort.direction == Direction.Output && candIsTransition && candidate.direction == Direction.Input) ||
                (startIsTransition && startPort.direction == Direction.Output && candIsState && candidate.direction == Direction.Input);

            if (valid) result.Add(candidate);
        }

        return result;
    }
    internal void DisconnectStateNode(BossStateNodeView stateView)
    {
        if (stateView == null || stateView.nodeData == null) return;

        var state = stateView.nodeData;

        // 1) Remove edges connected to this node (both in/out)
        var edges = this.edges.ToList()
            .Where(e => e.input?.node == stateView || e.output?.node == stateView)
            .ToList();
        foreach (var e in edges)
            RemoveElement(e); // triggers elementsToRemove -> keeps model clean as well

        // 2) Clear state's transitions
        if (state.transitions != null && state.transitions.Length > 0)
        {
            Undo.RecordObject(state, "Disconnect State");
            state.transitions = new StateTransition[0];
            EditorUtility.SetDirty(state);
        }

        // 3) Remove this state from all transitions' nextStates
        if (currentGraph?.stateNodes != null)
        {
            foreach (var s in currentGraph.stateNodes)
            {
                if (s == null || s.transitions == null) continue;
                foreach (var t in s.transitions)
                {
                    if (t == null || t.nextStates == null) continue;
                    var nexts = new List<BossStateNode>(t.nextStates);
                    if (nexts.Remove(state))
                    {
                        Undo.RecordObject(t, "Disconnect State from Transition");
                        t.nextStates = nexts.ToArray();
                        EditorUtility.SetDirty(t);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    // Disconnect a Transition node from everything and update data
    internal void DisconnectTransitionNode(StateTransitionNodeView transView)
    {
        if (transView == null || transView.transitionData == null) return;

        var trans = transView.transitionData;

        // 1) Remove edges connected to this node (both in/out)
        var edges = this.edges.ToList()
            .Where(e => e.input?.node == transView || e.output?.node == transView)
            .ToList();
        foreach (var e in edges)
            RemoveElement(e);

        // 2) Remove this transition from every state's transitions[]
        if (currentGraph?.stateNodes != null)
        {
            foreach (var s in currentGraph.stateNodes)
            {
                if (s == null || s.transitions == null) continue;
                var list = new List<StateTransition>(s.transitions);
                if (list.Remove(trans))
                {
                    Undo.RecordObject(s, "Disconnect Transition");
                    s.transitions = list.ToArray();
                    EditorUtility.SetDirty(s);
                }
            }
        }

        // 3) (Optional) Clear nextStates on the transition itself
        if (trans.nextStates != null && trans.nextStates.Length > 0)
        {
            Undo.RecordObject(trans, "Clear Transition NextStates");
            trans.nextStates = new BossStateNode[0];
            EditorUtility.SetDirty(trans);
        }

        AssetDatabase.SaveAssets();
    }
    // Sync edges <-> data
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        // Edge creations from default Edge (e.g., if listener didn't replace yet)
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                var fromState = edge.output?.node as BossStateNodeView;
                var toTrans = edge.input?.node as StateTransitionNodeView;

                var fromTrans = edge.output?.node as StateTransitionNodeView;
                var toState = edge.input?.node as BossStateNodeView;

                // A: State -> Transition
                if (fromState != null && toTrans != null)
                {
                    var state = fromState.nodeData;
                    var trans = toTrans.transitionData;

                    Undo.RecordObject(state, "Connect State → Transition");
                    var list = new List<StateTransition>(state.transitions ?? new StateTransition[0]);
                    if (!list.Contains(trans)) list.Add(trans);
                    state.transitions = list.ToArray();
                    EditorUtility.SetDirty(state);
                }

                // B: Transition -> State
                if (fromTrans != null && toState != null)
                {
                    var trans = fromTrans.transitionData;
                    var state = toState.nodeData;

                    Undo.RecordObject(trans, "Connect Transition → State");
                    var list = new List<BossStateNode>(trans.nextStates ?? new BossStateNode[0]);
                    if (!list.Contains(state)) list.Add(state);
                    trans.nextStates = list.ToArray();
                    EditorUtility.SetDirty(trans);
                }
            }
        }
        
        // Handle deletions (edges & nodes) and update arrays/assets
        if (change.elementsToRemove != null)
        {
            foreach (var el in change.elementsToRemove)
            {
                if (el is Edge edge)
                {
                    var fromState = edge.output?.node as BossStateNodeView;
                    var toTrans = edge.input?.node as StateTransitionNodeView;

                    var fromTrans = edge.output?.node as StateTransitionNodeView;
                    var toState = edge.input?.node as BossStateNodeView;

                    // Removing State -> Transition
                    if (fromState != null && toTrans != null)
                    {
                        var state = fromState.nodeData;
                        var trans = toTrans.transitionData;

                        Undo.RecordObject(state, "Disconnect State → Transition");
                        var list = new List<StateTransition>(state.transitions ?? new StateTransition[0]);
                        list.Remove(trans);
                        state.transitions = list.ToArray();
                        EditorUtility.SetDirty(state);
                    }

                    // Removing Transition -> State
                    if (fromTrans != null && toState != null)
                    {
                        var trans = fromTrans.transitionData;
                        var state = toState.nodeData;

                        Undo.RecordObject(trans, "Disconnect Transition → State");
                        var list = new List<BossStateNode>(trans.nextStates ?? new BossStateNode[0]);
                        list.Remove(state);
                        trans.nextStates = list.ToArray();
                        EditorUtility.SetDirty(trans);
                    }
                }
                else if (el is BossStateNodeView stateView)
                {
                    var state = stateView.nodeData;
                    if (currentGraph != null && state != null)
                    {
                        // Remove from graph
                        Undo.RecordObject(currentGraph, "Delete State Node");
                        var states = new List<BossStateNode>(currentGraph.stateNodes ?? new BossStateNode[0]);
                        states.Remove(state);
                        currentGraph.stateNodes = states.ToArray();
                        EditorUtility.SetDirty(currentGraph);

                        // Remove this state from any transition.nextStates
                        foreach (var s in states)
                        {
                            if (s?.transitions == null) continue;
                            foreach (var t in s.transitions)
                            {
                                if (t == null || t.nextStates == null) continue;
                                var nexts = new List<BossStateNode>(t.nextStates);
                                if (nexts.Remove(state))
                                {
                                    Undo.RecordObject(t, "Update Transition nextStates");
                                    t.nextStates = nexts.ToArray();
                                    EditorUtility.SetDirty(t);
                                }
                            }
                        }

                        // Delete the State asset itself
                        SafeDeleteAsset(state);
                    }
                }
                else if (el is StateTransitionNodeView transView)
{
    var trans = transView.transitionData;
    if (trans != null)
    {
        // Remove from all states' transitions[]
        if (currentGraph?.stateNodes != null)
        {
            foreach (var s in currentGraph.stateNodes)
            {
                if (s == null || s.transitions == null) continue;
                var list = new List<StateTransition>(s.transitions);
                if (list.Remove(trans))
                {
                    Undo.RecordObject(s, "Delete Transition");
                    s.transitions = list.ToArray();
                    EditorUtility.SetDirty(s);
                }
            }
        }

        // NEW: Remove from graph.transitionNodes[]
        if (currentGraph != null)
        {
            Undo.RecordObject(currentGraph, "Delete Transition Node");
            var tlist = new List<StateTransition>(currentGraph.transitionNodes ?? new StateTransition[0]);
            tlist.Remove(trans);
            currentGraph.transitionNodes = tlist.ToArray();
            EditorUtility.SetDirty(currentGraph);
        }

        // Delete the Transition asset (if you still want the asset removed on delete)
        SafeDeleteAsset(trans);
    }
}

            }
        }

        AssetDatabase.SaveAssets();
        return change;
    }

    private static void SafeDeleteAsset(Object obj)
    {
        if (obj == null) return;
        var path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
    }
}
