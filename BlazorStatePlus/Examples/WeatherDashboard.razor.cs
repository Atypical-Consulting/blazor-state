using BlazorStatePlus.Abstractions;
using Microsoft.Extensions.Logging;

namespace BlazorStatePlus.Examples;

public partial class WeatherDashboard : Components.PersistentComponentBase
{
    [Inject] private WeatherService Weather { get; set; } = null!;
    [Inject] private ILogger<WeatherDashboard> Logger { get; set; } = null!;

    private IStateSlice<WeatherForecast[]?> _forecasts = null!;

    protected override async Task OnInitializedAsync()
    {
        base.OnInitialized();

        // Create a slice with a 5-minute TTL.
        // On first prerender: calls the factory, stores the result.
        // On interactive load: restores the cached data, skips the HTTP call.
        // After 5 minutes: IsStale becomes true, UI can show a refresh prompt.
        _forecasts = await State.CreateAndInitAsync(
            "forecasts",
            () => Weather.GetForecastAsync(),
            o => o.TimeToLive = TimeSpan.FromMinutes(5));
    }

    private async Task Refresh()
    {
        _forecasts.Value = await Weather.GetForecastAsync();
        Logger.LogInformation("Forecasts refreshed at {Time}", DateTimeOffset.UtcNow);
    }
}