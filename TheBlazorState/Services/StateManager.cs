using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TheBlazorState.Abstractions;
using TheBlazorState.Configuration;
using TheBlazorState.Extensions;
using TheBlazorState.Storage;

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
    ILogger<StateManager> logger,
    TheBlazorStateOptions options,
    StorageStrategyInitializer initializer) : IDisposable
{
    private readonly List<PersistingComponentStateSubscription> _subscriptions = [];
    private readonly List<Func<Task>> _persistCallbacks = [];
    private readonly HashSet<string> _registeredKeys = [];
    private bool _registered;
    private bool _disposed;

    // Keep compiler happy — initializer is used to force DI ordering
    private readonly StorageStrategyInitializer _initializer = initializer;

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    /// <summary>
    /// Restores a property value from prerender state or server cache (synchronous path).
    /// Called by generated code during OnInitialized for [Persist] properties.
    /// The sync path (PersistentComponentState + IMemoryCache) ALWAYS runs regardless of strategy.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <param name="key">Unique persistence key for this property.</param>
    /// <param name="strategy">The configured storage strategy (may be null for default).</param>
    /// <param name="meta">The StateMeta companion for this property.</param>
    /// <param name="valueSetter">Action to set the backing field value.</param>
    /// <param name="valueGetter">Func to get the current backing field value.</param>
    public void RestoreProperty<T>(
        string key,
        IStorageStrategy? strategy,
        StateMeta meta,
        Action<T> valueSetter,
        Func<T> valueGetter)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Allow re-registration when a component re-mounts in the same circuit.
        _registeredKeys.Add(key);

        // Always register persist callback + eager change handler (before any early returns).
        RegisterPersistPropertyCallback(key, strategy, valueGetter);
        RegisterEagerChangeHandler(key, strategy, meta, valueGetter);

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
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or InvalidCastException)
        {
            logger.LogWarning(ex, "Property '{Key}': deserialization failed, using default", key);
        }

        logger.LogDebug("Property '{Key}': no persisted value, using default", key);
    }

    private void RegisterEagerChangeHandler<T>(
        string key, IStorageStrategy? strategy, StateMeta meta, Func<T> valueGetter)
    {
        var effectiveStrategy = strategy ?? options.DefaultStorage;
        meta.OnChanged += () =>
        {
            var value = valueGetter();
            var now = DateTimeOffset.UtcNow;

            // Always update server cache eagerly
            cache.Set(key, new PersistedEnvelope<T> { Value = value, PersistedAt = now }, CacheEntryOptions);

            // Write to browser strategy eagerly (fire-and-forget on circuit thread)
            if (effectiveStrategy is not PrerenderHtmlStrategy and not ServerMemoryCacheStrategy)
            {
                _ = PersistToBrowserStrategyAsync(key, value, now, effectiveStrategy, meta.TimeToLive);
            }
        };
    }

    /// <summary>
    /// Asynchronously restores a property value from a browser storage strategy (e.g., LocalStorage, SessionStorage, IndexedDB).
    /// Called by generated code during OnInitializedAsync for [Persist] properties.
    /// Skips if already restored or if strategy is PrerenderHtml/ServerMemoryCache (already handled sync).
    /// </summary>
    public async Task RestorePropertyAsync<T>(
        string key,
        IStorageStrategy? strategy,
        StateMeta meta,
        Action<T> valueSetter)
    {
        // Skip if already restored by the sync path
        if (meta.WasRestored)
            return;

        // Resolve effective strategy
        var effective = strategy ?? options.DefaultStorage;

        // Skip for strategies already handled by the sync RestoreProperty path
        if (effective is PrerenderHtmlStrategy or ServerMemoryCacheStrategy)
            return;

        try
        {
            var result = await effective.RestoreAsync<T>(key);
            if (result.Found && result.Value is not null)
            {
                var ttl = meta.TimeToLive;
                if (ttl.HasValue && result.PersistedAt.HasValue
                    && DateTimeOffset.UtcNow - result.PersistedAt.Value > ttl.Value)
                {
                    logger.LogDebug("Property '{Key}': async restored value discarded (TTL expired)", key);
                    return;
                }

                valueSetter(result.Value);
                meta.MarkRestored(result.PersistedAt ?? DateTimeOffset.UtcNow);
                logger.LogDebug("Property '{Key}': restored from {Strategy}", key, effective.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property '{Key}': async restore failed, using default", key);
        }
    }

    private void RegisterPersistPropertyCallback<T>(string key, IStorageStrategy? strategy, Func<T> valueGetter)
    {
        // Resolve effective strategy
        var effective = strategy ?? options.DefaultStorage;

        RegisterPersistCallback(async () =>
        {
            var envelope = new PersistedEnvelope<T>
            {
                Value = valueGetter(),
                PersistedAt = DateTimeOffset.UtcNow
            };

            // Always write to prerender HTML + server cache
            persistence.PersistAsJson(key, envelope);
            cache.Set(key, envelope, CacheEntryOptions);

            // Additionally write to the configured strategy if it's a browser strategy.
            // Skip during prerender — JSInterop isn't available. The eager OnChanged
            // handler writes to browser storage during interactive mode instead.
            if (effective is not (PrerenderHtmlStrategy or ServerMemoryCacheStrategy))
            {
                try
                {
                    var metadata = new StorageMetadata(key, null, envelope.PersistedAt);
                    await effective.PersistAsync(key, envelope.Value, metadata);
                }
                catch (InvalidOperationException)
                {
                    // Expected during prerender — JSInterop not yet available
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Property '{Key}': failed to persist to {Strategy}", key, effective.GetType().Name);
                }
            }
        });
    }

    private async Task PersistToBrowserStrategyAsync<T>(
        string key, T value, DateTimeOffset timestamp,
        IStorageStrategy strategy, TimeSpan? ttl)
    {
        try
        {
            var metadata = new StorageMetadata(key, ttl, timestamp);
            await strategy.PersistAsync(key, value, metadata);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property '{Key}': eager persist to {Strategy} failed",
                key, strategy.GetType().Name);
        }
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
