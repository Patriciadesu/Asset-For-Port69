using UnityEngine;

public class PlayerExtension : MonoBehaviour
{
    public virtual void OnStart(PlayerController player) { }
    public virtual void OnUpdate(PlayerController player) { }
    public virtual void OnEnterTrigger(PlayerController player) { }
    public virtual void OnStayTrigger(PlayerController player) { }
    public virtual void OnExitTrigger(PlayerController player) { }
    public virtual void OnEnterCollision(PlayerController player) { }
    public virtual void OnStayCollision(PlayerController player) { }
    public virtual void OnExitCollision(PlayerController player) { }
}






