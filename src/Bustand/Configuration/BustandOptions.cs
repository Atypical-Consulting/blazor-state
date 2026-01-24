using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Configuration;

/// <summary>
/// Configuration options for Bustand store registration.
/// </summary>
public class BustandOptions
{
    /// <summary>
    /// Assemblies to scan for stores marked with [BustandStore].
    /// If empty, scans the calling assembly.
    /// </summary>
    public List<Assembly> AssembliesToScan { get; } = new();

    /// <summary>
    /// Override the mode-aware default lifetime for all stores.
    /// Individual stores can still override via [BustandStore(Lifetime)].
    /// </summary>
    public ServiceLifetime? DefaultLifetimeOverride { get; set; }

    /// <summary>
    /// If true, logs a warning when Singleton stores are detected in Server mode.
    /// Default: true.
    /// </summary>
    public bool WarnOnSingletonInServerMode { get; set; } = true;

    /// <summary>
    /// Adds the assembly containing the specified type to the scan list.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to scan.</typeparam>
    /// <returns>This options instance for chaining.</returns>
    public BustandOptions ScanAssemblyContaining<T>()
    {
        AssembliesToScan.Add(typeof(T).Assembly);
        return this;
    }

    /// <summary>
    /// Adds the specified assembly to the scan list.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This options instance for chaining.</returns>
    public BustandOptions ScanAssembly(Assembly assembly)
    {
        AssembliesToScan.Add(assembly);
        return this;
    }
}
