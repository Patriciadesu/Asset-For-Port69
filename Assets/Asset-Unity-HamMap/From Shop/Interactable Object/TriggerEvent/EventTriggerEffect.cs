using UnityEngine.Events;

public class EventTriggerEffect : ObjectEffect
{
    public UnityEvent onPlayerTouched;
    public override void ApplyEffect(Player player)
    {
        base.ApplyEffect(player);
        onPlayerTouched.Invoke();
    }
}
