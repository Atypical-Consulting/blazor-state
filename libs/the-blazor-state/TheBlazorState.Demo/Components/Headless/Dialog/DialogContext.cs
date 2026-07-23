namespace TheBlazorState.Demo.Components.Headless.Dialog;

public class DialogContext
{
    public bool IsOpen { get; internal set; }
    public string TitleId { get; }
    public string ContentId { get; }
    public string? TriggerId { get; internal set; }
    public Func<Task> Open { get; }
    public Func<Task> Close { get; }
    public Func<Task> Toggle { get; }

    public DialogContext(string id, Func<Task> open, Func<Task> close, Func<Task> toggle)
    {
        TitleId = $"dialog-title-{id}";
        ContentId = $"dialog-content-{id}";
        Open = open;
        Close = close;
        Toggle = toggle;
    }
}
