using UnityEngine;

public class DialogTrigger : ObjectEffect
{
    [TextArea(3, 10), SerializeField] private string[] dialog;

    public void Start()
    {
        if (DialogManager.Instance == null)
        {
            this.gameObject.AddComponent<DialogManager>();
        }
    }
    public override void ApplyEffect(GameObject player)
    {
        if (player != null)
        {
            TriggerDialog();
            //Debug.Log($"{gameObject.name} triggered dialog - {player.gameObject.name} started dialog!");
        }
    }

    public void TriggerDialog()
    {
        DialogManager dialogManager = DialogManager.Instance;
        if (dialogManager != null)
        {
            dialogManager.StartDialog(dialog);
        }
        else
        {
            Debug.LogWarning("DialogManager not found in the scene.");
        }
    }
}
