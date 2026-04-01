using BlazorStatePlus.Abstractions;

namespace BlazorStatePlus.Services;

/// <summary>
/// Central service for creating and managing <see cref="IStateSlice{T}"/> instances.
/// Wraps <see cref="PersistentComponentState"/> with a friendlier API.
/// 
/// Inject this into components instead of using <c>PersistentComponentState</c> directly.
/// </summary>
public sealed class StateManager(PersistentComponentState persistence) : IDisposable
{
    private readonly List<PersistingComponentStateSubscription> _subscriptions = [];
    private readonly List<Func<Task>> _persistCallbacks = [];
    private bool _registered;
    private bool _disposed;

    /// <summary>
    /// Creates a state slice for a single value.
    /// Automatically tries to restore from prerendered state,
    /// and registers a callback to persist the current value.
    /// </summary>
    /// <typeparam name="T">Type of the value to persist (must be JSON-serializable).</typeparam>
    /// <param name="key">Unique key for this slice within the component.</param>
    /// <param name="defaultValue">Fallback value when nothing is restored.</param>
    /// <param name="configure">Optional configuration (TTL, AllowUpdates, etc.).</param>
    public IStateSlice<T> CreateSlice<T>(
        string key,
        T defaultValue = default!,
        Action<StateSliceOptions>? configure = null)
    {
        var options = BuildOptions(key, configure);

        var wasRestored = persistence.TryTakeFromJson<PersistedEnvelope<T>>(
            options.Key!, out var envelope);

        T restoredValue;
        bool effectivelyRestored;

        if (wasRestored && envelope is not null)
        {
            // Check if the persisted value has exceeded TTL
            if (options.TimeToLive.HasValue
                && DateTimeOffset.UtcNow - envelope.PersistedAt > options.TimeToLive.Value)
            {
                restoredValue = defaultValue;
                effectivelyRestored = false;
            }
            else
            {
                restoredValue = envelope.Value;
                effectivelyRestored = true;
            }
        }
        else
        {
            restoredValue = defaultValue;
            effectivelyRestored = false;
        }

        var slice = new StateSlice<T>(restoredValue, effectivelyRestored, options);

        RegisterPersistCallback(() =>
        {
            persistence.PersistAsJson(options.Key!, new PersistedEnvelope<T>
            {
                Value = slice.Value,
                PersistedAt = DateTimeOffset.UtcNow
            });
            return Task.CompletedTask;
        });

        return slice;
    }

    /// <summary>
    /// Convenience overload: creates a slice and immediately initializes it
    /// with a synchronous factory if no restored value exists.
    /// Combines <c>CreateSlice</c> + <c>InitializeIfNeeded</c> in one call.
    /// </summary>
    public IStateSlice<T> CreateAndInit<T>(
        string key,
        Func<T> factory,
        Action<StateSliceOptions>? configure = null)
    {
        var slice = CreateSlice<T>(key, default!, configure);
        if (!slice.WasRestored || slice.IsStale)
        {
            slice.InitializeIfNeeded(factory());
        }
        return slice;
    }

    /// <summary>
    /// Convenience overload: creates a slice and immediately initializes it
    /// with an async factory if no restored value exists.
    /// The factory is NOT called when a value was successfully restored.
    /// </summary>
    public async Task<IStateSlice<T>> CreateAndInitAsync<T>(
        string key,
        Func<Task<T>> factory,
        Action<StateSliceOptions>? configure = null)
    {
        var slice = CreateSlice<T>(key, default!, configure);
        await slice.InitializeIfNeededAsync(factory);
        return slice;
    }

    private void RegisterPersistCallback(Func<Task> callback)
    {
        _persistCallbacks.Add(callback);

        // Register a single OnPersisting subscription that calls all callbacks.
        // We defer this to avoid race conditions during shutdown.
        if (!_registered)
        {
            _registered = true;
            _subscriptions.Add(persistence.RegisterOnPersisting(async () =>
            {
                foreach (var cb in _persistCallbacks)
                    await cb();
            }));
        }
    }

    private static StateSliceOptions BuildOptions(string key, Action<StateSliceOptions>? configure)
    {
        var options = new StateSliceOptions { Key = key };
        configure?.Invoke(options);
        options.Key ??= key;
        return options;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sub in _subscriptions)
            sub.Dispose();

        _subscriptions.Clear();
        _persistCallbacks.Clear();
    }

    /// <summary>
    /// Internal envelope that wraps the persisted value with metadata.
    /// Stored as JSON in the prerendered HTML.
    /// </summary>
    internal sealed class PersistedEnvelope<T>
    {
        public T Value { get; init; } = default!;
        public DateTimeOffset PersistedAt { get; init; }
    }

}
