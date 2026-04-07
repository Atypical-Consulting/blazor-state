using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Shared;

public partial class TryItHint
{
    private static readonly HashSet<string> DismissedHints = new();
    [Parameter, EditorRequired] public string Text { get; set; } = "";
    [Parameter, EditorRequired] public string Id { get; set; } = "";
    private bool _dismissed;

    protected override void OnInitialized()
    {
        _dismissed = DismissedHints.Contains(Id);
    }

    private void Dismiss()
    {
        DismissedHints.Add(Id);
        _dismissed = true;
    }
}