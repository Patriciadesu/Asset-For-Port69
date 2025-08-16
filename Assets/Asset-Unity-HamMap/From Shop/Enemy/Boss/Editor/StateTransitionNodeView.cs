// At top of the file (if not already present)
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;            // Undo, EditorUtility, SerializedObject
using UnityEditor.UIElements; // PopupField, PropertyField
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class StateTransitionNodeView : Node
{
    public Port input;
    public Port output;
    public StateTransition transitionData;

    // NEW: SerializedObject reference + a container for condition fields UI
    private SerializedObject soTransition;
    private VisualElement conditionFieldsRoot;
    private Label conditionInfoLabel;

    public StateTransitionNodeView(StateTransition transition, IEdgeConnectorListener edgeListener = null)
    {
        this.transitionData = transition;
        title = "Transition";
        style.width = 260;
        style.backgroundColor = new Color(0.18f, 0.2f, 0.3f);

        // ---- Ports ----
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

        // ---- SerializedObject for binding ----
        soTransition = new SerializedObject(transitionData);

        // ---- Condition Type dropdown ----
        var conditionTypes = GetAllConditionTypes();
        var displayNames = conditionTypes.Select(TypeDisplayName).ToList();

        int currentIndex = -1;
        if (transitionData.condition != null)
        {
            var currentType = transitionData.condition.GetType();
            currentIndex = conditionTypes.FindIndex(t => t == currentType);
        }
        if (currentIndex < 0 && conditionTypes.Count > 0) currentIndex = 0;

        var popup = new PopupField<string>("Condition Type", displayNames,
            Mathf.Clamp(currentIndex, -1, displayNames.Count - 1));

        popup.RegisterValueChangedCallback(evt =>
        {
            int newIndex = displayNames.IndexOf(evt.newValue);
            if (newIndex < 0 || newIndex >= conditionTypes.Count) return;

            var newType = conditionTypes[newIndex];
            var newInstance = Activator.CreateInstance(newType) as Condition;
            if (newInstance == null)
            {
                Debug.LogError($"Failed to instantiate Condition type: {newType.FullName}. Ensure a public parameterless ctor.");
                return;
            }

            Undo.RecordObject(transitionData, "Change Condition Type");
            transitionData.condition = newInstance;
            EditorUtility.SetDirty(transitionData);

            // Refresh SerializedObject and rebuild fields panel + info label
            soTransition.Update();
            RebuildConditionFields();
        });

        mainContainer.Add(popup);


        // ---- Fields root (where we render the Condition's fields) ----
        conditionFieldsRoot = new VisualElement();
        conditionFieldsRoot.style.marginTop = 4;
        conditionFieldsRoot.style.marginBottom = 4;
        mainContainer.Add(conditionFieldsRoot);

        // If condition is null, initialize with first available type
        if (transitionData.condition == null && conditionTypes.Count > 0)
        {
            var t0 = conditionTypes[currentIndex];
            var inst = Activator.CreateInstance(t0) as Condition;
            if (inst != null)
            {
                Undo.RecordObject(transitionData, "Set Default Condition");
                transitionData.condition = inst;
                EditorUtility.SetDirty(transitionData);
                soTransition.Update();
            }
        }

        // Build the fields panel for current condition (if any)
        RebuildConditionFields();

        // Usual finishing touches
        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(UnityEngine.Random.Range(200, 650), UnityEngine.Random.Range(80, 450), 260, 180));

        // Context menu (Disconnect/Delete) — keep your existing items
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;
            evt.menu.AppendAction("Disconnect", _ => gv?.DisconnectTransitionNode(this));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => gv?.DeleteSelection());
        }));
    }

    // ---------- Helpers ----------


    private void RebuildConditionFields()
    {
        conditionFieldsRoot.Clear();

        // Get the managed reference property for condition
        if (soTransition == null) soTransition = new SerializedObject(transitionData);
        soTransition.Update();

        var condProp = soTransition.FindProperty("condition");
        if (condProp == null)
        {
            conditionFieldsRoot.Add(new Label("No condition property found."));
            return;
        }

        if (transitionData.condition == null)
        {
            conditionFieldsRoot.Add(new Label("No condition selected."));
            return;
        }

        // Option A (simple): draw the entire managed reference as one PropertyField
        // This may show Unity's built-in managed reference type selector; if you prefer only fields,
        // comment this and use Option B below.
        // var pf = new PropertyField(condProp, "Parameters");
        // pf.Bind(soTransition);
        // conditionFieldsRoot.Add(pf);
        // return;

        // Option B (custom): draw only the condition's child fields (skip managedReference metadata)
        var iterator = condProp.Copy();
        var end = iterator.GetEndProperty();

        // Move to first child
        bool enterChildren = true;
        int startDepth = -1;

        while (iterator.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(iterator, end))
                break;

            // First child encountered: record its depth so we limit to direct descendants
            if (startDepth < 0) startDepth = iterator.depth;

            // Stop if we moved out of the condition subtree or too deep
            if (iterator.depth < startDepth) break;
            if (!iterator.propertyPath.StartsWith(condProp.propertyPath))
                break;

            // Skip Unity's managed reference metadata
            if (iterator.name.StartsWith("managedReference"))
            {
                enterChildren = true;
                continue;
            }

            // Create a field for this property
            var childCopy = iterator.Copy(); // IMPORTANT: copy before iterator moves
            var field = new PropertyField(childCopy);
            field.Bind(soTransition);
            conditionFieldsRoot.Add(field);

            // For child properties, do not enter children (PropertyField will handle its subtree)
            enterChildren = false;
        }

        // Apply on change
        soTransition.ApplyModifiedProperties();
    }

    private static List<Type> GetAllConditionTypes()
    {
#if UNITY_EDITOR
        // Fast path in the editor
        var list = new List<Type>();
        foreach (var t in UnityEditor.TypeCache.GetTypesDerivedFrom<Condition>())
        {
            if (!t.IsAbstract && t.IsClass && HasPublicParameterlessCtor(t))
                list.Add(t);
        }
        return list.OrderBy(t => t.Name).ToList();
#else
        // Fallback reflection
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Condition).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && HasPublicParameterlessCtor(t))
            .OrderBy(t => t.Name)
            .ToList();
#endif
    }

    private static bool HasPublicParameterlessCtor(Type t)
        => t.GetConstructor(Type.EmptyTypes) != null;

    private static string TypeDisplayName(Type t) => t.Name;
    public void RefreshNodeFromData()
{
    if (transitionData == null) return;

    // Ensure SerializedObject exists & is current
    if (soTransition == null) soTransition = new SerializedObject(transitionData);
    else soTransition.Update();

    // Update header/title text if you want to reflect something dynamic
    title = "Transition";

    

    // Rebuild the fields panel for the currently selected Condition
    RebuildConditionFields();

    // If you keep a reference to the dropdown, keep it in sync too (optional)
    // if (conditionTypePopup != null)
    // {
    //     var types = GetAllConditionTypes();
    //     var currentType = transitionData.condition != null ? transitionData.condition.GetType() : null;
    //     var idx = currentType != null ? types.FindIndex(t => t == currentType) : -1;
    //     var name = (idx >= 0) ? TypeDisplayName(types[idx]) : "None";
    //     conditionTypePopup.SetValueWithoutNotify(name);
    // }

    RefreshExpandedState();
    RefreshPorts();
}

}
