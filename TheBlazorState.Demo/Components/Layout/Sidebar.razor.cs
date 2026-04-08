using Microsoft.AspNetCore.Components;
using TheBlazorState.Services;

namespace TheBlazorState.Demo.Components.Layout;

public partial class Sidebar : ComponentBase
{
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Inject] public StateManager StateManager { get; set; } = null!;

    [Parameter] public EventCallback OnClose { get; set; }

    private async Task ResetAllState()
    {
        await StateManager.ClearAllAsync();
        Nav.NavigateTo("/", forceLoad: true);
    }
}
