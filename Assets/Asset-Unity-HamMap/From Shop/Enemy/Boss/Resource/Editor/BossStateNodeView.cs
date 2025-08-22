using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Timeline;

public class BossStateNodeView : Node
{
    public Port input;
    public Port output;
    public BossStateNode nodeData;

    private readonly Action<BossStateNodeView> onSelected;

    private SerializedObject soStateNode;
    private PopupField<string> bossStateTypePopup;
    private VisualElement bossStateFieldsRoot;

    private VisualElement timelineDropZone;
    private ObjectField timelineObjectField;

    public BossStateNodeView(BossStateNode nodeData,
                             Action<BossStateNodeView> onSelected = null,
                             IEdgeConnectorListener edgeListener = null)
    {
        this.nodeData = nodeData;
        this.onSelected = onSelected;

        title = nodeData.stateName;
        style.width = 260;

        input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "";
        titleContainer.Add(input);

        output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "";
        extensionContainer.Add(output);

        if (edgeListener != null)
        {
            input.AddManipulator(new EdgeConnector<Edge>(edgeListener));
            output.AddManipulator(new EdgeConnector<Edge>(edgeListener));
        }

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

        var initToggle = new Toggle("Initial") { value = nodeData.isInitialState };
        initToggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == nodeData.isInitialState) return;
            Undo.RecordObject(nodeData, "Toggle Initial");
            nodeData.isInitialState = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(initToggle);

        BuildBossStateSection();

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(UnityEngine.Random.Range(50, 400), UnityEngine.Random.Range(50, 400), 260, 200));
        SetPosition(new Rect(nodeData.position, new Vector2(260, 200)));

        // On move, save back
        this.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            var rect = GetPosition();
            if (nodeData.position != rect.position)
            {
                Undo.RecordObject(nodeData, "Move State Node");
                nodeData.position = rect.position;
                EditorUtility.SetDirty(nodeData);
            }
        });
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;
            evt.menu.AppendAction("Disconnect", _ => gv?.DisconnectStateNode(this));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => gv?.DeleteSelection());
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

        if (soStateNode == null) soStateNode = new SerializedObject(nodeData);
        else soStateNode.Update();

        RebuildBossStateChildFields();

        System.Reflection.FieldInfo field = null;
        bool show = nodeData.state != null &&
                    TryGetTimelineFieldInfo(nodeData.state.GetType(), out field);

        if (timelineDropZone != null)
            timelineDropZone.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        if (timelineObjectField != null)
        {
            timelineObjectField.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show && field != null)
                timelineObjectField.SetValueWithoutNotify(field.GetValue(nodeData.state) as UnityEngine.Timeline.TimelineAsset);
        }

        RefreshExpandedState();
        RefreshPorts();
    }

    // ---------- BossState section ----------
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

    private void BuildBossStateSection()
    {
        soStateNode = new SerializedObject(nodeData);

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
            var newInstance = (BossState)FormatterServices.GetUninitializedObject(newType);
            newInstance.stateName = newType.Name;

            Undo.RecordObject(nodeData, "Change BossState Type");
            nodeData.state = newInstance;
            EditorUtility.SetDirty(nodeData);

            soStateNode.Update();
            RebuildBossStateChildFields();
        });
        mainContainer.Add(bossStateTypePopup);
        
        bossStateFieldsRoot = new VisualElement();
        bossStateFieldsRoot.style.marginTop = 4;
        bossStateFieldsRoot.style.marginBottom = 4;
        mainContainer.Add(bossStateFieldsRoot);

        if (nodeData.state == null && stateTypes.Count > 0)
        {
            var t0 = stateTypes[currentIndex];
            var inst = (BossState)FormatterServices.GetUninitializedObject(t0);
            inst.stateName = t0.Name;

            Undo.RecordObject(nodeData, "Set Default BossState");
            nodeData.state = inst;
            EditorUtility.SetDirty(nodeData);
            soStateNode.Update();
        }

        RebuildBossStateChildFields();
    }


    private void RebuildBossStateChildFields()
    {
        bossStateFieldsRoot.Clear();

        if (nodeData.state == null)
        {
            bossStateFieldsRoot.Add(new Label("No BossState selected."));
            return;
        }

        if (soStateNode == null) soStateNode = new SerializedObject(nodeData);
        soStateNode.Update();

        var stateProp = soStateNode.FindProperty("state");
        if (stateProp == null)
        {
            bossStateFieldsRoot.Add(new Label("No 'state' property found."));
            return;
        }

        var stateType = nodeData.state.GetType();
        var childNames = GetDerivedSerializedFieldNames(stateType, typeof(BossState));

        for (int i = 0; i < childNames.Count; i++)
        {
            var name = childNames[i];
            var p = stateProp.FindPropertyRelative(name);
            if (p != null)
            {
                var pf = new PropertyField(p);
                pf.Bind(soStateNode);
                bossStateFieldsRoot.Add(pf);
            }
        }

        soStateNode.ApplyModifiedProperties();
    }

    private static List<string> GetDerivedSerializedFieldNames(Type type, Type baseType)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var fields = type.GetFields(flags)
            .Where(f =>
                !f.IsStatic &&
                (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
            .Select(f => f.Name)
            .ToList();

        // explicit exclude base fields (safety)
        var baseFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(f => f.Name).ToHashSet();
        fields.RemoveAll(baseFields.Contains);
        return fields;
    }

    // ---------- Timeline drag/drop ----------
    private static bool TryGetTimelineFieldInfo(Type stateType, out FieldInfo field)
    {
        field = null;
        if (stateType == null) return false;

        var t = stateType;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        while (t != null && t != typeof(object))
        {
            var fi = t.GetFields(flags).FirstOrDefault(f =>
                f.FieldType == typeof(UnityEngine.Timeline.TimelineAsset) &&
                !f.IsStatic &&
                (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null));
            if (fi != null) { field = fi; return true; }
            t = t.BaseType;
        }
        return false;
    }
}