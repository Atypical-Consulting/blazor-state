namespace TheBlazorState.Demo.Components.Headless.Tabs;

public class TabsContext
{
    private readonly Action _stateChanged;
    private readonly Func<int, Task> _selectAsync;
    private int _tabCount;
    private int _panelCount;

    public string Id { get; }
    public int SelectedIndex { get; internal set; }
    public int TabCount => _tabCount;

    public TabsContext(string id, int selectedIndex, Func<int, Task> selectAsync, Action stateChanged)
    {
        Id = id;
        SelectedIndex = selectedIndex;
        _selectAsync = selectAsync;
        _stateChanged = stateChanged;
    }

    public int RegisterTab() => _tabCount++;

    public int RegisterPanel() => _panelCount++;

    public bool IsSelected(int index) => SelectedIndex == index;

    public async Task SelectAsync(int index)
    {
        await _selectAsync(index);
    }

    public string TabId(int index) => $"tab-{Id}-{index}";
    public string PanelId(int index) => $"tabpanel-{Id}-{index}";
}
