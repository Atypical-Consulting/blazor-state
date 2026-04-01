using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Attributes;
using BlazorStatePlus.Demo.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorStatePlus.Demo.Components.Pages;

public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = null!;
    [Inject] private ILogger<Weather> Logger { get; set; } = null!;

    [Slice(TimeToLive = "00:05:00")]
    private IStateSlice<WeatherForecast[]?> _forecasts = null!;

    partial void OnInitializeSlices(SliceInitContext ctx)
    {
        ctx.Forecasts
           .InitializeFrom(() => WeatherSvc.GetForecastAsync());
    }

    private async Task Refresh()
    {
        _forecasts.Value = await WeatherSvc.GetForecastAsync();
        Logger.LogInformation("Forecasts refreshed at {Time}", DateTimeOffset.UtcNow);
    }
}
