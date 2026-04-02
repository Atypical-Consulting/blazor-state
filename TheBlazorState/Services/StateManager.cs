using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TheBlazorState.Abstractions;

namespace TheBlazorState.Services;

/// <summary>
/// Central service for managing persisted state in Blazor components.
/// Wraps <see cref="PersistentComponentState"/> with a friendlier API,
/// and uses <see cref="IMemoryCache"/> to persist state across page reloads.
///
/// Inject this into components via the generated code (do not inject manually).
/// </summary>
public sealed class StateManager(
    PersistentComponentState persistence,
    IMemoryCache cache,
    ILogger<StateManager> logger) : IDisposable
{
    private readonly List<PersistingComponentStateSubscription> _subscriptions = [];
    private readonly List<Func<Task>> _persistCallbacks = [];
    private readonly HashSet<string> _registeredKeys = [];
    private bool _registered;
    private bool _disposed;

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    /// <summary>
    /// Restores a property value from prerender state or server cache.
    /// Called by generated code during OnInitialized for [Persist] properties.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <param name="key">Unique persistence key for this property.</param>
    /// <param name="meta">The StateMeta companion for this property.</param>
    /// <param name="valueSetter">Action to set the backing field value.</param>
    /// <param name="valueGetter">Func to get the current backing field value.</param>
    public void RestoreProperty<T>(
        string key,
        StateMeta meta,
        Action<T> valueSetter,
        Func<T> valueGetter)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_registeredKeys.Add(key))
            throw new InvalidOperationException(
                $"A property with key '{key}' has already been registered. Each key must be unique.");

        var ttl = meta.TimeToLive;

        // Try prerender state
        try
        {
            var wasRestored = persistence.TryTakeFromJson<PersistedEnvelope<T>>(key, out var envelope);
            if (wasRestored && envelope is not null)
            {
                if (ttl.HasValue && DateTimeOffset.UtcNow - envelope.PersistedAt > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': restored value discarded (TTL expired)", key);
                }
                else
                {
                    valueSetter(envelope.Value);
                    meta.MarkRestored(envelope.PersistedAt);
                    logger.LogDebug("Property '{Key}': restored from prerender", key);
                    RegisterPersistPropertyCallback(key, valueGetter);
                    return;
                }
            }

            // Try server cache
            if (cache.TryGetValue<PersistedEnvelope<T>>(key, out var cached) && cached is not null)
            {
                if (ttl.HasValue && DateTimeOffset.UtcNow - cached.PersistedAt > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': cached value discarded (TTL expired)", key);
                }
                else
                {
                    valueSetter(cached.Value);
                    meta.MarkRestored(cached.PersistedAt);
                    logger.LogDebug("Property '{Key}': restored from server cache", key);
                    RegisterPersistPropertyCallback(key, valueGetter);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property '{Key}': deserialization failed, using default", key);
        }

        logger.LogDebug("Property '{Key}': no persisted value, using default", key);
        RegisterPersistPropertyCallback(key, valueGetter);
    }

    private void RegisterPersistPropertyCallback<T>(string key, Func<T> valueGetter)
    {
        RegisterPersistCallback(() =>
        {
            var envelope = new PersistedEnvelope<T>
            {
                Value = valueGetter(),
                PersistedAt = DateTimeOffset.UtcNow
            };
            persistence.PersistAsJson(key, envelope);
            cache.Set(key, envelope, CacheEntryOptions);
            return Task.CompletedTask;
        });
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sub in _subscriptions)
            sub.Dispose();

        _subscriptions.Clear();
        _persistCallbacks.Clear();
        _registeredKeys.Clear();
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
