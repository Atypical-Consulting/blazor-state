using Bustand.DevTools.Middleware;
using Bustand.DevTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bustand.DevTools.Extensions;

/// <summary>
/// Extension methods for registering Bustand DevTools services.
/// </summary>
/// <remarks>
/// <para>
/// DevTools provides debugging capabilities for Bustand stores including:
/// </para>
/// <list type="bullet">
/// <item>State history tracking and inspection.</item>
/// <item>Time-travel debugging to restore historical states.</item>
/// <item>Real-time state change notifications.</item>
/// </list>
/// <para>
/// <b>Important:</b> DevTools should only be enabled in development environments
/// to avoid exposing sensitive state data in production.
/// </para>
/// </remarks>
public static class DevToolsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Bustand DevTools services with environment validation.
    /// Logs a warning and skips registration if not in development environment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The host environment for development check.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Recommended: Use the environment-aware overload
    /// builder.Services.AddBustandDevTools(builder.Environment);
    ///
    /// // Or manually check environment first:
    /// if (builder.Environment.IsDevelopment())
    /// {
    ///     builder.Services.AddBustandDevTools(builder.Environment);
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// This overload is the recommended approach as it provides automatic protection
    /// against accidentally enabling DevTools in production. When called outside of
    /// development, a warning is logged and services are not registered.
    /// </para>
    /// <para>
    /// Services registered:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="IDevToolsStore"/> as Singleton (shared across all circuits for development).</item>
    /// <item><see cref="DiffService"/> as Scoped for state comparison.</item>
    /// <item><c>DevToolsMiddleware&lt;TState&gt;</c> as Scoped (open generic, closed per-store by Bustand core).</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddBustandDevTools(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            Console.WriteLine(
                "[Bustand DevTools] WARNING: DevTools should only be enabled in Development. " +
                "Skipping registration for security.");
            return services;
        }

        return services.RegisterDevToolsServices();
    }

    /// <summary>
    /// Registers Bustand DevTools services without environment validation.
    /// Use this overload when you have already validated the environment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Only call when you've verified development environment:
    /// if (builder.Environment.IsDevelopment())
    /// {
    ///     builder.Services.AddBustandDevTools();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// <b>Warning:</b> This overload does not check the environment.
    /// The caller is responsible for ensuring DevTools is only enabled in development.
    /// </para>
    /// <para>
    /// Prefer the <see cref="AddBustandDevTools(IServiceCollection, IHostEnvironment)"/>
    /// overload for automatic environment protection.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddBustandDevTools(this IServiceCollection services)
    {
        // No environment check - caller is responsible for ensuring development environment
        return services.RegisterDevToolsServices();
    }

    /// <summary>
    /// Internal method that performs the actual service registration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Services registered:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="IDevToolsStore"/> as Singleton (shared across all circuits for development).</item>
    /// <item><see cref="DiffService"/> as Scoped for state comparison.</item>
    /// <item><see cref="DevToolsMiddleware{TState}"/> as Scoped (open generic, closed per-store by Bustand core).</item>
    /// </list>
    /// </remarks>
    private static IServiceCollection RegisterDevToolsServices(this IServiceCollection services)
    {
        // Register DevToolsStore as Singleton (shared across all circuits)
        // In development, this allows DevTools to track state changes across page navigations
        // and see history from all active stores, which is essential for debugging
        services.AddSingleton<IDevToolsStore, DevToolsStore>();

        // Register DiffService for state comparison in diff viewer panel
        services.AddScoped<DiffService>();

        // Register the open generic DevToolsMiddleware
        // The Bustand core will close it over each store's state type when creating pipelines
        // Registered as Scoped so each circuit gets its own middleware instances
        services.AddScoped(typeof(DevToolsMiddleware<>));

        return services;
    }
}
