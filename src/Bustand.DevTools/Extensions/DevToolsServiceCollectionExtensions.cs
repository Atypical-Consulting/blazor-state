using Microsoft.Extensions.DependencyInjection;

namespace Bustand.DevTools.Extensions;

/// <summary>
/// Extension methods for registering Bustand DevTools.
/// </summary>
public static class DevToolsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Bustand DevTools services.
    /// Call this only in development environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// if (builder.Environment.IsDevelopment())
    /// {
    ///     builder.Services.AddBustandDevTools();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// DevTools implementation will be added in Phase 5.
    /// This is a placeholder that does nothing for now.
    /// </remarks>
    public static IServiceCollection AddBustandDevTools(this IServiceCollection services)
    {
        // TODO: Phase 5 - Register DevTools middleware and services
        // - DevToolsMiddleware for state capture
        // - DevToolsStore for state history
        // - DevTools page component registration

        return services;
    }
}
