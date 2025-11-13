using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Warningvolumes))]
public class WarningvolumesEditor: Editor
{
    public override void OnInspectorGUI()
    {
        Warningvolumes script = (Warningvolumes)target;

        EditorGUILayout.LabelField("Toggle Scripts", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        bool newHeartbeat = EditorGUILayout.Toggle("HEARTBEAT", script.enableHEARTBEAT);
        bool newChaseTheme = EditorGUILayout.Toggle("CHASETHEME", script.enableCHASETHEME);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Toggle Scripts");
            script.enableHEARTBEAT = newHeartbeat;
            script.enableCHASETHEME = newChaseTheme;

            // ตรวจสอบและเพิ่ม/ลบ Component ตาม checkbox
            ToggleComponent<HEARTBEAT>(script.gameObject, newHeartbeat);
            ToggleComponent<CHASETHEME>(script.gameObject, newChaseTheme);

            EditorUtility.SetDirty(script);
        }
    }

    private void ToggleComponent<T>(GameObject obj, bool enabled) where T : Component
    {
        T existing = obj.GetComponent<T>();

        if (enabled && existing == null)
        {
            obj.AddComponent<T>();
        }
        else if (!enabled && existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
