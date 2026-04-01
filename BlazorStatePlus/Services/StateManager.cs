using BlazorStatePlus.Abstractions;
using Microsoft.Extensions.Logging;

namespace BlazorStatePlus.Services;

/// <summary>
/// Central service for creating and managing <see cref="IStateSlice{T}"/> instances.
/// Wraps <see cref="PersistentComponentState"/> with a friendlier API.
/// 
/// Inject this into components instead of using <c>PersistentComponentState</c> directly.
/// </summary>
public sealed class StateManager(
    PersistentComponentState persistence,
    ILogger<StateManager> logger) : IDisposable
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
    /// <remarks>
    /// <b>Security:</b> Slice values are serialized as JSON into the prerendered HTML response
    /// and are visible in the page source. Do not store sensitive data (auth tokens, PII, secrets)
    /// in state slices.
    /// </remarks>
    public IStateSlice<T> CreateSlice<T>(
        string key,
        T defaultValue = default!,
        Action<StateSliceOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var options = BuildOptions(key, configure);

        T restoredValue;
        bool effectivelyRestored;
        DateTimeOffset? persistedAt = null;

        try
        {
            var wasRestored = persistence.TryTakeFromJson<PersistedEnvelope<T>>(
                options.Key!, out var envelope);

            if (wasRestored && envelope is not null)
            {
                if (options.TimeToLive.HasValue
                    && DateTimeOffset.UtcNow - envelope.PersistedAt > options.TimeToLive.Value)
                {
                    restoredValue = defaultValue;
                    effectivelyRestored = false;
                    logger.LogDebug("Slice '{Key}': restored value discarded (TTL expired)", options.Key);
                }
                else
                {
                    restoredValue = envelope.Value;
                    effectivelyRestored = true;
                    persistedAt = envelope.PersistedAt;
                    logger.LogDebug("Slice '{Key}': restored from prerender", options.Key);
                }
            }
            else
            {
                restoredValue = defaultValue;
                effectivelyRestored = false;
                logger.LogDebug("Slice '{Key}': no persisted value, using default", options.Key);
            }
        }
        catch (Exception ex)
        {
            restoredValue = defaultValue;
            effectivelyRestored = false;
            logger.LogWarning(ex, "Slice '{Key}': deserialization failed, using default", options.Key);
        }

        var slice = new StateSlice<T>(restoredValue, effectivelyRestored, options, persistedAt);

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
                var snapshot = _persistCallbacks.ToArray();
                foreach (var cb in snapshot)
                    await cb();
            }));
        }
    }

    private static StateSliceOptions BuildOptions(string key, Action<StateSliceOptions>? configure)
    {
        var options = new StateSliceOptions { Key = key };
        configure?.Invoke(options);
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
