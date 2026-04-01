using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;
using Microsoft.Extensions.Logging;

namespace BlazorStatePlus.Examples;

public partial class WeatherDashboard : ComponentBase
{
    [Inject] private WeatherService Weather { get; set; } = null!;
    [Inject] private ILogger<WeatherDashboard> Logger { get; set; } = null!;

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<WeatherForecast[]?> _forecasts;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Forecasts
           .InitializeFrom(() => Weather.GetForecastAsync());
    }

    private async Task Refresh()
    {
        _forecasts.Value = await Weather.GetForecastAsync();
        Logger.LogInformation("Forecasts refreshed at {Time}", DateTimeOffset.UtcNow);
    }
}
