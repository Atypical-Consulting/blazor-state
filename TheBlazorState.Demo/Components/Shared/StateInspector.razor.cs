using Microsoft.AspNetCore.Components;
using TheBlazorState.Abstractions;
using TheBlazorState.Demo.Services;

namespace TheBlazorState.Demo.Components.Shared;

public record StateInspectorEntry(string Name, string Strategy, StateMeta Meta);

public partial class StateInspector : IDisposable
{
    [Inject] private StateInspectorService InspectorService { get; set; } = null!;

    private static bool _hasBeenOpened;
    private bool _open;
    private IReadOnlyList<StateInspectorEntry> _entries = [];
    private bool _hasRestored;

    protected override void OnInitialized()
    {
        if (!_hasBeenOpened)
        {
            _open = true;
            _hasBeenOpened = true;
        }
        InspectorService.OnChanged += OnEntriesChanged;
        RefreshEntries();
    }

    private void OnEntriesChanged()
    {
        RefreshEntries();
        InvokeAsync(StateHasChanged);
    }

    private void RefreshEntries()
    {
        _entries = InspectorService.Entries;
        _hasRestored = _entries.Any(e => e.Meta.WasRestored);
    }

    private void Open() => _open = true;
    private void Close() => _open = false;

    private static string FormatTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        return time.LocalDateTime.ToString("HH:mm:ss");
    }

    public void Dispose()
    {
        InspectorService.OnChanged -= OnEntriesChanged;
    }
}
