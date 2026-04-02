using TheBlazorState.Attributes;
using TheBlazorState.Demo.Services;
using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Weather : ComponentBase
{
    [Inject] private WeatherService WeatherSvc { get; set; } = null!;
    [Inject] private ILogger<Weather> Logger { get; set; } = null!;

    [Persist(TimeToLive = "00:05:00")]
    public partial WeatherForecast[]? Forecasts { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.Forecasts.LoadFrom(() => WeatherSvc.GetForecastAsync());
    }

    private async Task Refresh()
    {
        Forecasts = await WeatherSvc.GetForecastAsync();
        Logger.LogInformation("Forecasts refreshed at {Time}", DateTimeOffset.UtcNow);
    }
}
