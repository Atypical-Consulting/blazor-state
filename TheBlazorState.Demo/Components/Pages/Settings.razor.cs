using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Settings : ComponentBase
{
    [Inject] public ThemeState Theme { get; set; } = default!;

    [Persist]
    public partial string? SavedTheme { get; set; }

    [Persist]
    public partial string? SavedDensity { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
        ctx.SavedDensity.Storage = StorageStrategy.LocalStorage();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && SavedThemeMeta?.WasRestored == true)
        {
            if (SavedTheme is not null) Theme.Theme = SavedTheme;
            if (SavedDensity is not null) Theme.Density = SavedDensity;
            StateHasChanged();
        }
    }

    private void SetTheme(string theme)
    {
        Theme.Theme = theme;
        SavedTheme = theme;
    }

    private void SetDensity(string density)
    {
        Theme.Density = density;
        SavedDensity = density;
    }

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("SavedTheme", "LocalStorage", SavedThemeMeta),
        new("SavedDensity", "LocalStorage", SavedDensityMeta)
    ];
}
