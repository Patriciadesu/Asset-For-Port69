using UnityEngine.UIElements;
using System.Collections;

public interface IUseStamina
{
    public bool isUsingStamina { get; }
    public bool canDrainStamina { get; }
    void DrainStamina(float amount);
}

public interface ICancleGravity
{
    public bool canApplyGravity { get; set; }
}

public interface IInteractable
{
    void Interact();
}
public interface INodeInspectorContributor
{
    /// Build your node inspector controls under `container`.
    /// Implementers should keep references to their own fields if they need to toggle visibility later.
    void BuildInspectorUI(VisualElement container);

    /// Called by the node view when values change and you want to re-check visibility / refresh.
    void RefreshInspectorUI();
}

public interface IPlayerUISetter
{
    public void OnStart(PlayerUIManager playerUI);
}

public interface IInteruptPlayerMovement
{
    public bool isPerforming { get; }
}
public interface IUsableItem
{
    public IEnumerator Use() { yield return null; }
}
public interface IPerformOnCollect
{
    public IEnumerator OnCollect() { yield return null; }
}
public interface IEnemy
{
    public void TakeDamage(float damage) { }
}