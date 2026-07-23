namespace TheBlazorState.Demo.Components.Headless.Toggle;

public class ToggleContext
{
    public bool Value { get; internal set; }
    public Func<Task> Toggle { get; }

    public ToggleContext(bool value, Func<Task> toggle)
    {
        Value = value;
        Toggle = toggle;
    }
}
