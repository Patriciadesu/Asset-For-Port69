public class StateMachine
{
    private IState current;
    public IState Current => current;

    public void SetState(IState next)
    {
        if (current == next) return;
        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void Tick() => current?.Tick();
}
