using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Layout;

public partial class Sidebar : ComponentBase
{
    [Inject] public ProjectService ProjectSvc { get; set; } = default!;
    [Inject] public ProjectState ProjectState { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    [Parameter] public EventCallback OnClose { get; set; }

    [Persist]
    public partial int? SavedProjectId { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedProjectId.Storage = StorageStrategy.LocalStorage();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && SavedProjectIdMeta?.WasRestored == true && SavedProjectId is not null)
        {
            var project = ProjectSvc.GetAll().FirstOrDefault(p => p.Id == SavedProjectId);
            if (project is not null)
            {
                ProjectState.SelectedProject = project;
                StateHasChanged();
            }
        }
    }

    private void SelectProject(Project project)
    {
        ProjectState.SelectedProject = project;
        SavedProjectId = project.Id;
    }

    private async Task ResetAllState()
    {
        await JS.InvokeVoidAsync("clearAllBlazorState");
        Nav.NavigateTo("/", forceLoad: true);
    }
}
