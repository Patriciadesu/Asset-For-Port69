using UnityEngine;
using UnityEngine.Events;

public class EventTriggerEffect : ObjectEffect
{
    public UnityEvent onPlayerTouched;
    public override void ApplyEffect(GameObject player)
    {
        base.ApplyEffect(player);
        onPlayerTouched.Invoke();
    }
}
