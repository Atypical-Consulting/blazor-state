using System.Reflection;
using Bustand.Attributes;
using Bustand.Blazor;
using Bustand.Configuration;
using Bustand.Core;
using Bustand.Detection;
using Bustand.Middleware;
using Bustand.Persistence;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

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
        // Also trigger pipeline wiring if DevTools is enabled (even without other middleware)
        if (options.MiddlewareTypes.Count > 0 || options.DevToolsEnabled)
        {
            RegisterMiddlewareAndPipelines(services, assemblies, options.MiddlewareTypes, options);
        }

        // Auto-register persistence for [Persist] stores even without other middleware
        if (options.MiddlewareTypes.Count == 0 && !options.DevToolsEnabled)
        {
            var persistentStores = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<PersistAttribute>() != null &&
                           typeof(IStore).IsAssignableFrom(t))
                .ToList();

            if (persistentStores.Count > 0)
            {
                RegisterPersistenceServices(services, options);
                RegisterPersistenceForStores(services, persistentStores, options);
            }
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
        List<Type> middlewareTypes,
        BustandOptions options)
    {
        // Register each middleware type as transient
        foreach (var middlewareType in middlewareTypes)
        {
            if (!middlewareType.IsGenericTypeDefinition)
            {
                services.TryAddTransient(middlewareType);
            }
        }

        // Check if any stores have [Persist] attribute
        var hasPersistentStores = assemblies
            .SelectMany(a => a.GetTypes())
            .Any(t => t.GetCustomAttribute<PersistAttribute>() != null);

        if (hasPersistentStores)
        {
            RegisterPersistenceServices(services, options);
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

            // Check for [Persist] attribute on this store
            var persistAttr = storeType.GetCustomAttribute<PersistAttribute>();

            // Re-register store with a factory that injects the pipeline (and persistence)
            var middlewareTypesCopy = middlewareTypes.ToList();
            var optionsCopy = options; // Capture for closure

            services.Add(new ServiceDescriptor(
                storeType,
                sp => CreateStoreWithPipeline(sp, storeType, stateType, middlewareTypesCopy, optionsCopy, persistAttr),
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

    /// <summary>
    /// Registers browser storage services for persistence.
    /// Called internally by AddBustand when any stores have [Persist] attribute.
    /// </summary>
    private static void RegisterPersistenceServices(
        IServiceCollection services,
        BustandOptions options)
    {
        // Register IBrowserStorage as scoped (one per circuit/user)
        services.TryAddScoped<IBrowserStorage>(sp =>
        {
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            return new BrowserStorageService(jsRuntime, options.JsonSerializerOptions);
        });

        // Register circuit handler for Blazor Server reconnect handling
        // Note: In WASM, CircuitHandler services are never resolved, so this registration
        // is harmless but unused. In Server mode, this enables reconnect detection.
        services.TryAddScoped<CircuitHandler, BustandCircuitHandler>();
    }

    private static object CreateStoreWithPipeline(
        IServiceProvider sp,
        Type storeType,
        Type stateType,
        List<Type> middlewareTypes,
        BustandOptions options,
        PersistAttribute? persistAttr)
    {
        var store = ActivatorUtilities.CreateInstance(sp, storeType);

        // Build middleware list including persistence if applicable
        var middlewareInterfaceType = typeof(IMiddleware<>).MakeGenericType(stateType);
        var middlewareListType = typeof(List<>).MakeGenericType(middlewareInterfaceType);
        var middlewares = (System.Collections.IList)Activator.CreateInstance(middlewareListType)!;

        // Add user-configured middleware
        foreach (var mwType in middlewareTypes)
        {
            var concreteType = mwType.IsGenericTypeDefinition
                ? mwType.MakeGenericType(stateType)
                : mwType;

            if (!middlewareInterfaceType.IsAssignableFrom(concreteType))
                continue;

            var middleware = sp.GetService(concreteType);
            if (middleware != null)
            {
                // Pass store instance to middleware if it supports SetStoreInstance
                // This enables DevToolsMiddleware to register stores for time-travel
                TrySetStoreInstance(middleware, store);
                middlewares.Add(middleware);
            }
        }

        // If DevTools is enabled, try to add DevToolsMiddleware dynamically
        if (options.DevToolsEnabled)
        {
            TryAddDevToolsMiddleware(sp, store, stateType, middlewareInterfaceType, middlewares);
        }

        // Handle persistence if [Persist] attribute is present
        object? persistenceMiddleware = null;
        if (persistAttr != null)
        {
            var storage = sp.GetService<IBrowserStorage>();
            if (storage != null)
            {
                var storageKey = options.BuildStorageKey(storeType, persistAttr.Key);

                // Create PersistenceMiddleware<TState>
                var persistenceType = typeof(PersistenceMiddleware<>).MakeGenericType(stateType);
                persistenceMiddleware = Activator.CreateInstance(
                    persistenceType,
                    storage,
                    storageKey,
                    persistAttr.Storage,
                    options.PersistenceDebounceMs);

                // Add to pipeline
                middlewares.Add(persistenceMiddleware!);

                // Try to restore state (if storage is available)
                TryRestoreState(store, persistenceMiddleware!, stateType);
            }
        }

        // Create and inject the pipeline
        var pipelineType = typeof(MiddlewarePipeline<>).MakeGenericType(stateType);
        var pipeline = Activator.CreateInstance(pipelineType, middlewares);

        var setPipelineMethod = store.GetType().GetMethod(
            "SetPipeline",
            BindingFlags.Instance | BindingFlags.NonPublic);
        setPipelineMethod?.Invoke(store, new[] { pipeline });

        return store;
    }

    private static void TryRestoreState(object store, object persistenceMiddleware, Type stateType)
    {
        try
        {
            // Get RestoreStateAsync method
            var restoreMethod = persistenceMiddleware.GetType().GetMethod("RestoreStateAsync");
            if (restoreMethod == null)
                return;

            // Call RestoreStateAsync and get the task
            var task = (Task?)restoreMethod.Invoke(persistenceMiddleware, null);
            if (task == null)
                return;

            // We can't await in a factory, but we can try to get the result if storage is immediately available
            // This works for WASM where storage is synchronous-ish
            // For Server mode during prerender, this will return null (which is fine - InitialState is used)
            if (task.IsCompleted)
            {
                var resultProperty = task.GetType().GetProperty("Result");
                var restoredState = resultProperty?.GetValue(task);

                if (restoredState != null)
                {
                    // Call SetRestoredState on the store
                    var setRestoredMethod = store.GetType().GetMethod(
                        "SetRestoredState",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    setRestoredMethod?.Invoke(store, new[] { restoredState });
                }
            }
        }
        catch (Exception ex)
        {
            // Log and continue - InitialState will be used
            System.Diagnostics.Debug.WriteLine($"[Bustand] State restore failed: {ex.Message}. Using InitialState.");
        }
    }

    private static void RegisterPersistenceForStores(
        IServiceCollection services,
        List<Type> persistentStores,
        BustandOptions options)
    {
        foreach (var storeType in persistentStores)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == storeType);
            if (descriptor == null)
                continue;

            services.Remove(descriptor);

            var stateType = GetStoreStateType(storeType);
            if (stateType == null)
                continue;

            var persistAttr = storeType.GetCustomAttribute<PersistAttribute>()!;

            services.Add(new ServiceDescriptor(
                storeType,
                sp => CreateStoreWithPipeline(sp, storeType, stateType, new List<Type>(), options, persistAttr),
                descriptor.Lifetime));
        }
    }

    /// <summary>
    /// Attempts to call SetStoreInstance on middleware that supports it.
    /// This enables DevToolsMiddleware to register stores for time-travel functionality.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="store">The store instance to pass.</param>
    private static void TrySetStoreInstance(object middleware, object store)
    {
        try
        {
            // Look for SetStoreInstance method (internal or public)
            var setStoreMethod = middleware.GetType().GetMethod(
                "SetStoreInstance",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            setStoreMethod?.Invoke(middleware, new[] { store });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Bustand] Failed to set store instance on middleware: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to add DevToolsMiddleware dynamically when DevTools is enabled.
    /// Uses reflection to find the middleware type in the Bustand.DevTools assembly.
    /// </summary>
    /// <param name="sp">The service provider.</param>
    /// <param name="store">The store instance.</param>
    /// <param name="stateType">The state type for the store.</param>
    /// <param name="middlewareInterfaceType">The IMiddleware&lt;TState&gt; interface type.</param>
    /// <param name="middlewares">The middleware collection to add to.</param>
    private static void TryAddDevToolsMiddleware(
        IServiceProvider sp,
        object store,
        Type stateType,
        Type middlewareInterfaceType,
        System.Collections.IList middlewares)
    {
        try
        {
            // Try to find DevToolsMiddleware type by name
            // Assembly name: Bustand.DevTools
            // Type name: Bustand.DevTools.Middleware.DevToolsMiddleware`1
            var devToolsMiddlewareOpenType = Type.GetType(
                "Bustand.DevTools.Middleware.DevToolsMiddleware`1, Bustand.DevTools");

            if (devToolsMiddlewareOpenType == null)
            {
                // DevTools assembly not loaded - that's fine, just skip
                return;
            }

            // Close the generic type over the state type
            var devToolsMiddlewareType = devToolsMiddlewareOpenType.MakeGenericType(stateType);

            // Verify it implements IMiddleware<TState>
            if (!middlewareInterfaceType.IsAssignableFrom(devToolsMiddlewareType))
            {
                return;
            }

            // Try to resolve from DI
            var devToolsMiddleware = sp.GetService(devToolsMiddlewareType);
            if (devToolsMiddleware != null)
            {
                // Pass store instance to middleware
                TrySetStoreInstance(devToolsMiddleware, store);
                middlewares.Add(devToolsMiddleware);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Bustand] Failed to add DevTools middleware: {ex.Message}");
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
