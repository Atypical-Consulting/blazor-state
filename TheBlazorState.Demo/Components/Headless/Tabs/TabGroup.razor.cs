using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Tabs;

public partial class TabGroup : HeadlessBase
{
    [Parameter]
    public int SelectedIndex { get; set; }

    [Parameter]
    public EventCallback<int> SelectedIndexChanged { get; set; }

    [Parameter]
    public int DefaultIndex { get; set; }

    private TabsContext _context = null!;
    private string _id = null!;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _id = Guid.NewGuid().ToString("N")[..8];

        if (!_initialized)
        {
            SelectedIndex = DefaultIndex;
            _initialized = true;
        }

        _context = new TabsContext(_id, SelectedIndex, HandleSelect, StateHasChanged);
    }

    protected override void OnParametersSet()
    {
        _context.SelectedIndex = SelectedIndex;
    }

    private async Task HandleSelect(int index)
    {
        SelectedIndex = index;
        _context.SelectedIndex = index;
        await SelectedIndexChanged.InvokeAsync(index);
        StateHasChanged();
    }
}
