namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public class DropdownContext
{
    private readonly Action _stateChanged;
    private readonly List<DropdownItemRegistration> _items = [];

    public bool IsOpen { get; internal set; }
    public int FocusedIndex { get; internal set; } = -1;
    public string Id { get; }
    public Func<Task> Open { get; }
    public Func<Task> Close { get; }
    public Func<Task> Toggle { get; }
    public int ItemCount => _items.Count;

    public DropdownContext(string id, Func<Task> open, Func<Task> close, Func<Task> toggle, Action stateChanged)
    {
        Id = id;
        Open = open;
        Close = close;
        Toggle = toggle;
        _stateChanged = stateChanged;
    }

    public int RegisterItem(string? label, bool disabled)
    {
        var index = _items.Count;
        _items.Add(new DropdownItemRegistration(label, disabled));
        return index;
    }

    public bool IsItemDisabled(int index) =>
        index >= 0 && index < _items.Count && _items[index].Disabled;

    public bool IsFocused(int index) => FocusedIndex == index;

    public string TriggerId => $"dropdown-trigger-{Id}";
    public string PanelId => $"dropdown-panel-{Id}";
    public string ItemId(int index) => $"dropdown-item-{Id}-{index}";

    public int FindNextEnabledItem(int from, bool forward)
    {
        var count = _items.Count;
        if (count == 0) return -1;

        for (var i = 1; i <= count; i++)
        {
            var index = forward
                ? (from + i) % count
                : (from - i + count) % count;

            if (!_items[index].Disabled)
                return index;
        }

        return from;
    }

    private record DropdownItemRegistration(string? Label, bool Disabled);
}
