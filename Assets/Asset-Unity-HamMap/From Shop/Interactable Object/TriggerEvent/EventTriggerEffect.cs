using UnityEngine.Events;

public class EventTriggerEffect : ObjectEffect
{
    public UnityEvent events;
    public override void ApplyEffect(Player player)
    {
        base.ApplyEffect(player);
        events.Invoke();
    }
}
