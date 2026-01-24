using System.Reflection;
using Bustand.Attributes;
using Bustand.Configuration;
using Bustand.Core;
using Bustand.Detection;
using Bustand.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Register middleware types and wire pipelines
        if (options.MiddlewareTypes.Count > 0)
        {
            RegisterMiddlewareAndPipelines(services, assemblies, options.MiddlewareTypes);
        }

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

    private static void RegisterMiddlewareAndPipelines(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        List<Type> middlewareTypes)
    {
        // Register each middleware type as transient
        foreach (var middlewareType in middlewareTypes)
        {
            // For open generic types like LoggingMiddleware<>, we need to register per-state-type
            // For closed types, register directly
            if (!middlewareType.IsGenericTypeDefinition)
            {
                services.TryAddTransient(middlewareType);
            }
            // Open generics will be closed and registered per-store below
        }

        // Find all store types to wrap their registrations with pipeline injection
        var storeTypes = services
            .Where(d => typeof(IStore).IsAssignableFrom(d.ServiceType) && d.ServiceType.IsClass)
            .Select(d => d.ServiceType)
            .ToList();

        foreach (var storeType in storeTypes)
        {
            var descriptor = services.First(d => d.ServiceType == storeType);
            services.Remove(descriptor);

            // Get the state type from the store's base class
            var stateType = GetStoreStateType(storeType);
            if (stateType == null)
                continue;

            // Register closed middleware types for this state type
            foreach (var middlewareType in middlewareTypes)
            {
                if (middlewareType.IsGenericTypeDefinition)
                {
                    var closedType = middlewareType.MakeGenericType(stateType);
                    services.TryAddTransient(closedType);
                }
            }

            // Re-register store with a factory that injects the pipeline
            var middlewareTypesCopy = middlewareTypes.ToList(); // Capture for closure
            services.Add(new ServiceDescriptor(
                storeType,
                sp =>
                {
                    var store = ActivatorUtilities.CreateInstance(sp, storeType);
                    InjectPipeline(store, sp, middlewareTypesCopy, stateType);
                    return store;
                },
                descriptor.Lifetime));
        }
    }

    private static Type? GetStoreStateType(Type storeType)
    {
        // Walk up the type hierarchy to find ZustandStore<TState>
        var baseType = storeType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(ZustandStore<>))
            {
                return baseType.GetGenericArguments()[0];
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    private static void InjectPipeline(
        object store,
        IServiceProvider sp,
        List<Type> middlewareTypes,
        Type stateType)
    {
        // Build middleware list
        var middlewareInterfaceType = typeof(IMiddleware<>).MakeGenericType(stateType);
        var middlewareListType = typeof(List<>).MakeGenericType(middlewareInterfaceType);
        var middlewares = (System.Collections.IList)Activator.CreateInstance(middlewareListType)!;

        foreach (var mwType in middlewareTypes)
        {
            var concreteType = mwType.IsGenericTypeDefinition
                ? mwType.MakeGenericType(stateType)
                : mwType;

            // Check if this middleware implements IMiddleware<TState>
            if (!middlewareInterfaceType.IsAssignableFrom(concreteType))
                continue;

            var middleware = sp.GetService(concreteType);
            if (middleware != null)
            {
                middlewares.Add(middleware);
            }
        }

        // Create the pipeline
        var pipelineType = typeof(MiddlewarePipeline<>).MakeGenericType(stateType);
        var pipeline = Activator.CreateInstance(pipelineType, middlewares);

        // Call SetPipeline via reflection
        var setPipelineMethod = store.GetType().GetMethod(
            "SetPipeline",
            BindingFlags.Instance | BindingFlags.NonPublic);
        setPipelineMethod?.Invoke(store, new[] { pipeline });
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
