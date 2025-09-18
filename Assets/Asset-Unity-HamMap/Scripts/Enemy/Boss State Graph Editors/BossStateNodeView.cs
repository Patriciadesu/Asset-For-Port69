using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BossStateNodeView : Node
{
    public BossStateNode nodeData;
    public Port input;
    public Port output;

    private readonly Action<BossStateNodeView> onSelected;
    private readonly IEdgeConnectorListener edgeListener;

    private Editor _cachedSOEditor;
    private IMGUIContainer _inspectorIMGUI;
    private Button _swapBtn;

    private bool _portsSwappedUI; // visual toggle state only

    private VisualElement inspectorRoot;

    public BossStateNodeView(BossStateNode nodeData,
                             Action<BossStateNodeView> onSelected = null,
                             IEdgeConnectorListener edgeListener = null)
    {
        this.nodeData = nodeData;
        this.onSelected = onSelected;
        this.edgeListener = edgeListener;

        title = nodeData != null ? (nodeData.stateName ?? nodeData.name ?? "State") : "State";
        style.width = 320;

        // Respect saved node position on create
        var startPos = nodeData != null ? nodeData.position : Vector2.zero;
        SetPosition(new Rect(startPos, new Vector2(320, 220)));

        BuildTopSelectors(); // BossState type dropdown (auto-detects)
        BuildPorts();
        var spacerTop = new VisualElement();
        spacerTop.style.height = 5;
        mainContainer.Add(spacerTop);

        inspectorRoot = new VisualElement();
        var fold = new Foldout { text = "Inspector", value = true };
        fold.Add(inspectorRoot);
        mainContainer.Add(fold);
        BuildInspector();    // full inspector
        RefreshNodeFromData();

        RefreshExpandedState();
        RefreshPorts();
        var spacerBottom = new VisualElement();
        spacerBottom.style.height = 15;
        mainContainer.Add(spacerBottom);
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        if (nodeData != null)
        {
            if (nodeData.position != newPos.position)
            {
                Undo.RecordObject(nodeData, "Move State Node");
                nodeData.position = newPos.position;
                EditorUtility.SetDirty(nodeData);
            }
        }
    }

    public override void OnSelected()
    {
        base.OnSelected();
        onSelected?.Invoke(this);
    }

    // ---------- BossState Type dropdown ----------
    private void BuildTopSelectors()
    {
        var spacer = new VisualElement();
        spacer.style.height = 20;  // 10 px tall blank space
        mainContainer.Add(spacer);
        var nameField = new TextField("Name") { value = nodeData.stateName };
        nameField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == nodeData.stateName) return;
            Undo.RecordObject(nodeData, "Edit State Name");
            nodeData.stateName = evt.newValue;
            title = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(nameField);

        // Initial toggle
        var initToggle = new Toggle("Initial") { value = nodeData.isInitialState };
        initToggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == nodeData.isInitialState) return;
            Undo.RecordObject(nodeData, "Toggle Initial");
            nodeData.isInitialState = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(initToggle);

        // BossState type + auto fields
        BuildBossStateSection();
    }
    private PopupField<string> bossStateTypePopup;
    private VisualElement bossStateFieldsRoot;
    private void BuildBossStateSection()
    {
        var soStateNode = new SerializedObject(nodeData);

        var stateTypes = GetAllBossStateTypes();
        var display = stateTypes.Select(t => t.Name).ToList();

        int currentIndex = -1;
        if (nodeData.state != null)
        {
            var ct = nodeData.state.GetType();
            currentIndex = stateTypes.FindIndex(t => t == ct);
        }
        if (currentIndex < 0 && stateTypes.Count > 0) currentIndex = 0;

        bossStateTypePopup = new PopupField<string>("Boss State Type", display,
            Mathf.Clamp(currentIndex, -1, display.Count - 1));
        bossStateTypePopup.RegisterValueChangedCallback(evt =>
        {
            int newIndex = display.IndexOf(evt.newValue);
            if (newIndex < 0 || newIndex >= stateTypes.Count) return;

            var newType = stateTypes[newIndex];
            var newInstance = (BossState)Activator.CreateInstance(newType, new object[] { null });
            newInstance.stateName = newType.Name;

            Undo.RecordObject(nodeData, "Change BossState Type");
            nodeData.state = newInstance;
            EditorUtility.SetDirty(nodeData);

            soStateNode.Update();
            RefreshNodeFromData(); // update timeline UI etc.
            BuildInspector();      // refresh interface-driven UI
        });
        mainContainer.Add(bossStateTypePopup);

        mainContainer.Add(bossStateFieldsRoot);

        // Ensure we have an instance by default
        if (nodeData.state == null && stateTypes.Count > 0)
        {
            var t0 = stateTypes[currentIndex];
            var inst = (BossState)Activator.CreateInstance(t0, new object[] { null });
            inst.stateName = t0.Name;

            Undo.RecordObject(nodeData, "Set Default BossState");
            nodeData.state = inst;
            EditorUtility.SetDirty(nodeData);
            soStateNode.Update();
        }

    }
    private static List<Type> _cachedBossStateTypes;
    private static List<Type> GetAllBossStateTypes()
    {
        if (_cachedBossStateTypes != null) return _cachedBossStateTypes;
        _cachedBossStateTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<BossState>()
            .Where(t => t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();
        return _cachedBossStateTypes;
    }

    private static SerializedProperty TryFindProp(SerializedObject so, params string[] names)
    {
        foreach (var n in names)
        {
            var p = so.FindProperty(n);
            if (p != null) return p;
        }
        return null;
    }

    private PopupField<string> BuildTypeDropdown(string label,
        string[] baseTypeCandidates, SerializedProperty backingStringProp)
    {
        var baseType = FindTypeByName(baseTypeCandidates);
        if (baseType == null || backingStringProp == null || backingStringProp.propertyType != SerializedPropertyType.String)
            return null;

        var all = TypeCache.GetTypesDerivedFrom(baseType)
                           .Where(t => !t.IsAbstract && !t.IsGenericType)
                           .Select(t => t.FullName)
                           .OrderBy(n => n).ToList();

        if (all.Count == 0) return null;

        var current = string.IsNullOrEmpty(backingStringProp.stringValue)
            ? all[0] : (all.Contains(backingStringProp.stringValue) ? backingStringProp.stringValue : all[0]);

        var popup = new PopupField<string>(label: "", choices: all, defaultIndex: all.IndexOf(current));
        popup.tooltip = $"Select {label}";
        popup.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == backingStringProp.stringValue) return;
            var so = backingStringProp.serializedObject;
            so.Update();
            backingStringProp.stringValue = evt.newValue;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
        });

        return popup;
    }

    private static Type FindTypeByName(string[] simpleNames)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type hit = null;
            foreach (var name in simpleNames)
            {
                hit = asm.GetTypes().FirstOrDefault(t => t.Name == name);
                if (hit != null) return hit;
            }
        }
        return null;
    }

    // ---------- Ports + swap ----------
    private void BuildPorts()
    {
        var inColor = new Color(0.35f, 0.7f, 1f);
        var outColor = new Color(1f, 0.65f, 0.25f);

        input = PortUI.Make(this, Direction.Input, Port.Capacity.Multi,
                 "From Transition", "Transition → State", inColor, edgeListener);
        output = PortUI.Make(this, Direction.Output, Port.Capacity.Multi,
                 "To Transition", "State → Transition", outColor, edgeListener);

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

// Apply persisted side from asset on load
if (nodeData != null && nodeData.outputOnLeft) // output should be on LEFT
{
    // if it's not currently on left, swap once to honor saved state
    if (output.parent != inputContainer)
    {
        SwapPortsUtility.SwapSidesPersistent(
            this, ref input, ref output,
            inputContainer, outputContainer,
            nodeData,
            saveOutputOnLeft: val => nodeData.outputOnLeft = val
        );
    }
}
    }

    private void DoSwap()
{
    SwapPortsUtility.SwapSidesPersistent(
    this, ref input, ref output,
    inputContainer, outputContainer,
    nodeData,
    saveOutputOnLeft: val => nodeData.outputOnLeft = val
);

PortUI.UpdateBadge(input);
PortUI.UpdateBadge(output);
}

    // ---------- Embedded full inspector ----------
    private void BuildInspector()
    {
        inspectorRoot.Clear();

        // Prefer your contributor hook if present
        if (nodeData?.state is INodeInspectorContributor contributor)
        {
            contributor.BuildInspectorUI(inspectorRoot);
            return;
        }

        if (nodeData == null)
        {
            inspectorRoot.Add(new Label("No state asset."));
            return;
        }

        // 🔧 Bind to the ScriptableObject (UnityEngine.Object), not nodeData.state
        var so = new SerializedObject(nodeData);

        // Try common field names for the managed-reference BossState
        SerializedProperty stateProp = null;
        foreach (var name in new[] { "state", "bossState", "State", "BossState" })
        {
            stateProp = so.FindProperty(name);
            if (stateProp != null) break;
        }

        if (stateProp == null)
        {
            inspectorRoot.Add(new Label("No BossState managed reference found on node."));
            return;
        }

        static bool ShouldSkip(string n)
        {
            n = n?.ToLowerInvariant();
            return n == "onstatechanged" || n == "transitions" || n == "nextstates";
        }

        // Iterate fields under the managed reference and draw them
        var it = stateProp.Copy();
        var end = it.GetEndProperty();
        bool enterChildren = true;
        int startDepth = -1;

        while (it.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(it, end)) break;

            // stop once we leave the managed-reference subtree
            if (startDepth < 0) startDepth = it.depth;
            if (it.depth < startDepth) break;
            if (!it.propertyPath.StartsWith(stateProp.propertyPath)) break;

            if (it.propertyPath.EndsWith("m_Script") || ShouldSkip(it.name))
            {
                enterChildren = false;
                continue;
            }

            var field = new PropertyField(it.Copy());
            field.Bind(so);                  // ✅ bind to nodeData SerializedObject
            inspectorRoot.Add(field);

            enterChildren = false;
        }

        so.ApplyModifiedProperties();
    }


    public void RefreshNodeFromData()
    {
        if (nodeData != null)
            title = nodeData.stateName ?? nodeData.name ?? "State";

        PortUI.UpdateBadge(input);
        PortUI.UpdateBadge(output);

        _inspectorIMGUI?.MarkDirtyRepaint();
        RefreshExpandedState();
        RefreshPorts();
    }
}
