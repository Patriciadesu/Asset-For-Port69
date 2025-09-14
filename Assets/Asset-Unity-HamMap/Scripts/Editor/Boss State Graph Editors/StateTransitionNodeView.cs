using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class StateTransitionNodeView : Node
{
    public StateTransition transitionData;
    public Port input;
    public Port output;
    private VisualElement conditionFieldsRoot;
    private readonly IEdgeConnectorListener edgeListener;
    private PopupField<string> conditionTypePopup;
    private Editor _cachedSOEditor;
    private IMGUIContainer _inspectorIMGUI;
    private Button _swapBtn;
    private bool _portsSwappedUI;

    public StateTransitionNodeView(StateTransition transitionData,
                                   IEdgeConnectorListener edgeListener = null)
    {
        this.transitionData = transitionData;
        this.edgeListener = edgeListener;

        title = transitionData != null ? (transitionData.name ?? "Transition") : "Transition";
        style.width = 320;

        // Respect saved node position on create
        var startPos = transitionData != null ? transitionData.position : Vector2.zero;
        SetPosition(new Rect(startPos, new Vector2(320, 220)));

        BuildTopSelectors(); // Condition Type dropdown (auto-detects)
        BuildPorts();
        // Spacer before the foldout
        var spacerTop = new VisualElement();
        spacerTop.style.height = 5;
        mainContainer.Add(spacerTop);

        // Foldout with condition fields
        conditionFieldsRoot = new VisualElement();
        var fold = new Foldout { text = "Inspector", value = true };
        fold.Add(conditionFieldsRoot);
        mainContainer.Add(fold);

        // Build the fields inside
        RebuildConditionFields();
        RefreshNodeFromData();
        RefreshExpandedState();
        RefreshPorts();

        // Spacer after the foldout
        var spacerBottom = new VisualElement();
        spacerBottom.style.height = 15;
        mainContainer.Add(spacerBottom);

    }
    private void RebuildConditionFields()
    {
        conditionFieldsRoot.Clear();
        if (transitionData == null)
        {
            conditionFieldsRoot.Add(new Label("No transition."));
            return;
        }

        var so = new SerializedObject(transitionData);
        var condProp = so.FindProperty("condition");
        if (condProp == null)
        {
            conditionFieldsRoot.Add(new Label("No 'condition' property found."));
            return;
        }

        static bool ShouldSkip(string n)
        {
            n = n?.ToLowerInvariant();
            return n == "onstatechanged" || n == "transitions" || n == "nextstates";
        }

        var it = condProp.Copy();
        var end = it.GetEndProperty();
        bool enterChildren = true;
        int startDepth = -1;

        while (it.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(it, end)) break;
            if (startDepth < 0) startDepth = it.depth;
            if (it.depth < startDepth) break;
            if (!it.propertyPath.StartsWith(condProp.propertyPath)) break;

            if (it.propertyPath == "m_Script" || ShouldSkip(it.name))
            {
                enterChildren = false;
                continue;
            }

            var copy = it.Copy();
            var field = new PropertyField(copy);
            field.Bind(so);
            conditionFieldsRoot.Add(field);

            enterChildren = false;
        }

        so.ApplyModifiedProperties();
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        if (transitionData != null)
        {
            if (transitionData.position != newPos.position)
            {
                Undo.RecordObject(transitionData, "Move Transition Node");
                transitionData.position = newPos.position;
                EditorUtility.SetDirty(transitionData);
            }
        }
    }

    // ---------- Condition Type dropdown ----------
    private void BuildTopSelectors()
    {
        var spacer = new VisualElement();
        spacer.style.height = 20;  // 10 px tall blank space
        mainContainer.Add(spacer);
        var soTransition = new SerializedObject(transitionData);
        var types = GetAllConditionTypes();
        var names = types.Select(t => t.Name).ToList();
        int currentIndex = -1;
        if (transitionData.condition != null)
        {
            var ct = transitionData.condition.GetType();
            currentIndex = types.FindIndex(t => t == ct);
        }
        if (currentIndex < 0 && types.Count > 0) currentIndex = 0;

        conditionTypePopup = new PopupField<string>("Condition Type", names,
            Mathf.Clamp(currentIndex, -1, names.Count - 1));
        conditionTypePopup.RegisterValueChangedCallback(evt =>
        {
            int newIndex = names.IndexOf(evt.newValue);
            if (newIndex < 0 || newIndex >= types.Count) return;

            var newType = types[newIndex];
            var newInstance = Activator.CreateInstance(newType) as Condition;
            if (newInstance == null) return;

            Undo.RecordObject(transitionData, "Change Condition Type");
            transitionData.condition = newInstance;
            EditorUtility.SetDirty(transitionData);

            soTransition.Update();
            RebuildConditionFields();
        });
        mainContainer.Add(conditionTypePopup);
    }
    private static List<Type> _cachedConditionTypes;
    private static List<Type> GetAllConditionTypes()
    {
        if (_cachedConditionTypes != null) return _cachedConditionTypes;
        _cachedConditionTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<Condition>()
            .Where(t => !t.IsAbstract && t.IsClass && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToList();
        return _cachedConditionTypes;
    }



    // ---------- Ports + swap ----------
    private void BuildPorts()
    {
        var inColor = new Color(0.8f, 0.55f, 1f);
        var outColor = new Color(0.5f, 1f, 0.6f);

        input = PortUI.Make(this, Direction.Input, Port.Capacity.Multi,
                 "From State", "State → Transition", inColor, edgeListener);
        output = PortUI.Make(this, Direction.Output, Port.Capacity.Multi,
                 "To State(s)", "Transition → State(s)", outColor, edgeListener);

        inputContainer.Add(input);
        outputContainer.Add(output);

        _swapBtn = new Button(() => DoSwap()) { text = "⇄" };
        _swapBtn.tooltip = "Swap input/output sides AND roles";
        _swapBtn.pickingMode = PickingMode.Position;
        _swapBtn.style.flexShrink = 0;
        _swapBtn.style.marginLeft = 6;
        titleContainer.Add(_swapBtn);
        _swapBtn.BringToFront();
        PortUI.AlignToParentSide(input,  inputContainer);
        PortUI.AlignToParentSide(output, outputContainer);

        if (transitionData != null && transitionData.outputOnLeft)
        {
            if (output.parent != inputContainer)
            {
                SwapPortsUtility.SwapSidesPersistent(
                    this, ref input, ref output,
                    inputContainer, outputContainer,
                    transitionData,
                    saveOutputOnLeft: val => transitionData.outputOnLeft = val
                );
            }
        }
    }

    private void DoSwap()
{
    SwapPortsUtility.SwapSidesPersistent(
    this, ref input, ref output,
    inputContainer, outputContainer,
    transitionData,
    saveOutputOnLeft: val => transitionData.outputOnLeft = val
);

PortUI.UpdateBadge(input);
PortUI.UpdateBadge(output);
}

    // ---------- Embedded full inspector ----------
    public void RefreshNodeFromData()
    {
        if (transitionData != null && !string.IsNullOrEmpty(transitionData.name))
            title = transitionData.name;

        PortUI.UpdateBadge(input);
        PortUI.UpdateBadge(output);

        _inspectorIMGUI?.MarkDirtyRepaint();
        RefreshExpandedState();
        RefreshPorts();
    }
}
