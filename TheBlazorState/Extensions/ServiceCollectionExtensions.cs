using TheBlazorState.Configuration;
using TheBlazorState.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TheBlazorState.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers TheBlazorState services: StateManager, storage strategies, and options.
    /// </summary>
    public static IServiceCollection AddTheBlazorState(
        this IServiceCollection services,
        Action<TheBlazorStateOptions>? configure = null)
    {
        var options = new TheBlazorStateOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddScoped<StateManager>();
        return services;
    }
}
