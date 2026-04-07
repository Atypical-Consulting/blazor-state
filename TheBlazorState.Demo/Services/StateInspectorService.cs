using TheBlazorState.Demo.Components.Shared;

namespace TheBlazorState.Demo.Services;

public class StateInspectorService
{
    private readonly Dictionary<string, List<StateInspectorEntry>> _pages = new();
    private string? _activePage;

    public event Action? OnChanged;

    public IReadOnlyList<StateInspectorEntry> Entries =>
        _activePage is not null && _pages.TryGetValue(_activePage, out var entries)
            ? entries
            : [];

    public void Register(string pageKey, List<StateInspectorEntry> entries)
    {
        _activePage = pageKey;
        _pages[pageKey] = entries;
        OnChanged?.Invoke();
    }
}
