using Microsoft.AspNetCore.Components;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;

namespace TheBlazorState.Demo.Components.Layout;

public partial class MainLayout
{
    [Inject] private ThemeState Theme { get; set; } = null!;
    [Inject] private AppJsModule AppJs { get; set; } = null!;

    private bool _sidebarOpen;
    private string _lastTheme = "";
    private string _lastDensity = "";
    private void ToggleSidebar() => _sidebarOpen = !_sidebarOpen;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Read saved theme from localStorage to initialize ThemeState.
            // The inline script in App.razor already applied the correct CSS class,
            // so we only need to sync the C# state here (no setThemeClass call).
            var savedTheme = await AppJs.GetStoredPreferenceAsync("Settings.SavedTheme");
            var savedDensity = await AppJs.GetStoredPreferenceAsync("Settings.SavedDensity");

            if (savedTheme is not null) Theme.Theme = savedTheme;
            if (savedDensity is not null) Theme.Density = savedDensity;

            _lastTheme = Theme.Theme;
            _lastDensity = Theme.Density;
            StateHasChanged();
            return;
        }

        if (Theme.Theme != _lastTheme)
        {
            _lastTheme = Theme.Theme;
            await AppJs.SetThemeClassAsync(Theme.Theme);
        }

        if (Theme.Density != _lastDensity)
        {
            _lastDensity = Theme.Density;
            await AppJs.SetDensityClassAsync(Theme.Density);
        }
    }
}