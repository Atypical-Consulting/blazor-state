using BlazorStatePlus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorStatePlus.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="StateManager"/> as a scoped service.
    /// Call this in your <c>Program.cs</c> to enable the library.
    /// 
    /// <example>
    /// <code>
    /// builder.Services.AddBlazorStatePlus();
    /// </code>
    /// </example>
    /// </summary>
    public static IServiceCollection AddBlazorStatePlus(this IServiceCollection services)
    {
        services.AddScoped<StateManager>();
        return services;
    }
}
