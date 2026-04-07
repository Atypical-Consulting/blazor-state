using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;
using TheBlazorState.Demo.Services;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Settings : ComponentBase
{
    [Inject] public ThemeState Theme { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] private StateInspectorService Inspector { get; set; } = default!;

    [Persist]
    public partial string? SavedTheme { get; set; }

    [Persist]
    public partial string? SavedDensity { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
        ctx.SavedDensity.Storage = StorageStrategy.LocalStorage();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && SavedThemeMeta?.WasRestored == true)
        {
            if (SavedTheme is not null)
            {
                Theme.Theme = SavedTheme;
                await JS.InvokeVoidAsync("setThemeClass", SavedTheme);
            }
            if (SavedDensity is not null)
            {
                Theme.Density = SavedDensity;
                await JS.InvokeVoidAsync("setDensityClass", SavedDensity);
            }
            StateHasChanged();
        }
    }

    private async Task SetTheme(string theme)
    {
        Theme.Theme = theme;
        SavedTheme = theme;
        await JS.InvokeVoidAsync("setThemeClass", theme);
    }

    private async Task SetDensity(string density)
    {
        Theme.Density = density;
        SavedDensity = density;
        await JS.InvokeVoidAsync("setDensityClass", density);
    }

    protected override void OnParametersSet()
    {
        Inspector.Register("Settings",
        [
            new("SavedTheme", "LocalStorage", SavedThemeMeta),
            new("SavedDensity", "LocalStorage", SavedDensityMeta)
        ]);
    }

}
