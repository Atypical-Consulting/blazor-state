using Microsoft.AspNetCore.Components;
using TheBlazorState.Abstractions;

namespace TheBlazorState.Demo.Components.Shared;

public partial class StateInspector : ComponentBase
{
    [Parameter, EditorRequired]
    public List<StateInspectorEntry> Entries { get; set; } = [];

    private bool _open = false;

    private void Toggle() => _open = !_open;

    private static string FormatTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        return time.LocalDateTime.ToString("HH:mm:ss");
    }
}

public record StateInspectorEntry(string Name, string Strategy, StateMeta Meta);
