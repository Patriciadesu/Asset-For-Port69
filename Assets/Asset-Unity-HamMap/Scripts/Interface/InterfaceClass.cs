using UnityEngine;
using NaughtyAttributes;

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
    void  Interact();
}