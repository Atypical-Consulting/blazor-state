using System.Reflection;
using Bustand.Attributes;
using Bustand.Configuration;
using Bustand.Core;
using Bustand.Detection;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Extensions;

/// <summary>
/// Extension methods for registering Bustand stores with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all stores marked with [BustandStore] attribute.
    /// Stores are registered with mode-aware lifetime by default:
    /// Singleton in WebAssembly, Scoped in Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Basic usage - scans calling assembly
    /// services.AddBustand();
    ///
    /// // With configuration
    /// services.AddBustand(options =>
    /// {
    ///     options.ScanAssemblyContaining&lt;MyStore&gt;();
    ///     options.DefaultLifetimeOverride = ServiceLifetime.Scoped;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBustand(
        this IServiceCollection services,
        Action<BustandOptions>? configure = null)
    {
        var options = new BustandOptions();
        configure?.Invoke(options);

        // Default to calling assembly if none specified
        var assemblies = options.AssembliesToScan.Count > 0
            ? options.AssembliesToScan
            : new List<Assembly> { Assembly.GetCallingAssembly() };

        // Determine default lifetime based on mode
        var defaultLifetime = options.DefaultLifetimeOverride
            ?? BlazorModeDetector.RecommendedStoreLifetime;

        // Warn about Singleton in Server mode (potential data leak)
        if (!BlazorModeDetector.IsWebAssembly &&
            defaultLifetime == ServiceLifetime.Singleton &&
            options.WarnOnSingletonInServerMode)
        {
            Console.WriteLine(
                "[Bustand Warning] Singleton stores in Server mode may leak data between users/circuits. " +
                "Consider using Scoped lifetime or set WarnOnSingletonInServerMode = false if intentional.");
        }

        // Use Scrutor for assembly scanning
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes
                .WithAttribute<BustandStoreAttribute>()
                .AssignableTo(typeof(IStore)))
            .AsSelf()
            .WithLifetime(defaultLifetime));

        // Post-process to apply per-store lifetime overrides
        ApplyPerStoreLifetimeOverrides(services, assemblies, defaultLifetime, options.WarnOnSingletonInServerMode);

        return services;
    }

    private static void ApplyPerStoreLifetimeOverrides(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime defaultLifetime,
        bool warnOnSingletonInServerMode)
    {
        // Find stores with explicit lifetime in attribute
        var storeTypesWithOverrides = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Type = t,
                Attribute = t.GetCustomAttribute<BustandStoreAttribute>()
            })
            .Where(x => x.Attribute?.Lifetime != null && x.Attribute.Lifetime != defaultLifetime)
            .ToList();

        foreach (var store in storeTypesWithOverrides)
        {
            var explicitLifetime = store.Attribute!.Lifetime!.Value;

            // Warn about explicit Singleton in Server mode
            if (!BlazorModeDetector.IsWebAssembly &&
                explicitLifetime == ServiceLifetime.Singleton &&
                warnOnSingletonInServerMode)
            {
                Console.WriteLine(
                    $"[Bustand Warning] Store '{store.Type.Name}' uses explicit Singleton lifetime in Server mode. " +
                    "This may leak data between users/circuits.");
            }

            // Remove existing registration and re-add with correct lifetime
            var existing = services.FirstOrDefault(d => d.ServiceType == store.Type);
            if (existing != null)
            {
                services.Remove(existing);
                services.Add(new ServiceDescriptor(store.Type, store.Type, explicitLifetime));
            }
        }
    }

    /// <summary>
    /// Verifies that ApplyPerStoreLifetimeOverrides correctly modifies service descriptors.
    /// This is an internal test helper exposed for unit testing.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    /// <param name="storeType">The store type to check.</param>
    /// <returns>The lifetime of the registered store, or null if not found.</returns>
    internal static ServiceLifetime? GetRegisteredLifetime(
        this IServiceCollection services,
        Type storeType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == storeType);
        return descriptor?.Lifetime;
    }
}
