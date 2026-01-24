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
    /// <item><see cref="IDevToolsStore"/> as Scoped (one instance per circuit/user).</item>
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
    private static IServiceCollection RegisterDevToolsServices(this IServiceCollection services)
    {
        // Register DevToolsStore as Scoped (one per circuit/user)
        // This ensures each user gets their own state history
        services.AddScoped<IDevToolsStore, DevToolsStore>();

        return services;
    }
}
