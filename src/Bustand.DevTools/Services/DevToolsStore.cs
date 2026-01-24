using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bustand.Core;
using Bustand.DevTools.Models;

namespace Bustand.DevTools.Services;

/// <summary>
/// Implementation of <see cref="IDevToolsStore"/> that manages state history and time-travel debugging.
/// </summary>
/// <remarks>
/// <para>
/// <b>Key features:</b>
/// </para>
/// <list type="bullet">
/// <item>Records state changes per store with a limit of 100 entries.</item>
/// <item>Supports time-travel by restoring historical states.</item>
/// <item>Handles circular references gracefully during JSON serialization.</item>
/// <item>Notifies subscribers via <see cref="StateHistoryChanged"/> event.</item>
/// </list>
/// </remarks>
public sealed class DevToolsStore : IDevToolsStore
{
    /// <summary>
    /// Maximum number of history entries to keep per store.
    /// </summary>
    private const int MaxHistoryLength = 100;

    /// <summary>
    /// Per-store state history, keyed by store name.
    /// </summary>
    private readonly Dictionary<string, List<StateSnapshot>> _history = new();

    /// <summary>
    /// Current time-travel position per store.
    /// </summary>
    private readonly Dictionary<string, int> _currentIndex = new();

    /// <summary>
    /// Registered store instances for time-travel operations.
    /// </summary>
    private readonly Dictionary<string, IStore> _stores = new();

    /// <summary>
    /// JSON serializer options for state serialization.
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Flag to prevent recording state changes during time-travel.
    /// </summary>
    private bool _isTimeTraveling;

    /// <inheritdoc />
    public event EventHandler? StateHistoryChanged;

    /// <inheritdoc />
    public IReadOnlyList<string> RegisteredStoreNames => _history.Keys.ToList();

    /// <inheritdoc />
    public bool IsTimeTraveling => _isTimeTraveling;

    /// <summary>
    /// Initializes a new instance of <see cref="DevToolsStore"/>.
    /// </summary>
    public DevToolsStore()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public void RecordStateChange(
        Type storeType,
        object oldState,
        object newState,
        string? actionName,
        DateTimeOffset timestamp)
    {
        // Skip recording during time-travel to prevent polluting history
        if (_isTimeTraveling)
        {
            return;
        }

        var storeName = storeType.Name;

        // Initialize history for new stores
        if (!_history.TryGetValue(storeName, out var history))
        {
            history = new List<StateSnapshot>();
            _history[storeName] = history;
            _currentIndex[storeName] = -1;
        }

        // Truncate future history if we were time-traveling and now making new changes
        // This creates a new branch from the current position
        var currentIdx = _currentIndex[storeName];
        if (currentIdx >= 0 && currentIdx < history.Count - 1)
        {
            history.RemoveRange(currentIdx + 1, history.Count - currentIdx - 1);
        }

        // Enforce max history length by removing oldest entries
        while (history.Count >= MaxHistoryLength)
        {
            history.RemoveAt(0);
        }

        // Serialize state to JSON with error handling for circular references
        string stateJson;
        try
        {
            stateJson = JsonSerializer.Serialize(newState, _jsonOptions);
        }
        catch (Exception ex)
        {
            stateJson = JsonSerializer.Serialize(new
            {
                error = "Serialization failed",
                message = ex.Message
            }, _jsonOptions);
        }

        // Create and add the snapshot
        var snapshot = new StateSnapshot(
            Index: history.Count,
            State: newState,
            ActionName: actionName,
            Timestamp: timestamp,
            StateJson: stateJson
        );

        history.Add(snapshot);
        _currentIndex[storeName] = history.Count - 1;

        // Notify subscribers
        StateHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IReadOnlyList<StateSnapshot> GetHistory(string storeName)
    {
        if (!_history.TryGetValue(storeName, out var history))
        {
            return Array.Empty<StateSnapshot>();
        }

        return history.AsReadOnly();
    }

    /// <inheritdoc />
    public StateSnapshot? GetCurrentSnapshot(string storeName)
    {
        if (!_history.TryGetValue(storeName, out var history) || history.Count == 0)
        {
            return null;
        }

        var idx = _currentIndex.GetValueOrDefault(storeName, history.Count - 1);

        // Ensure index is within bounds
        if (idx < 0 || idx >= history.Count)
        {
            idx = history.Count - 1;
        }

        return history[idx];
    }

    /// <inheritdoc />
    public void JumpToState(string storeName, int index)
    {
        // Validate store exists
        if (!_history.TryGetValue(storeName, out var history))
        {
            return;
        }

        // Validate index bounds
        if (index < 0 || index >= history.Count)
        {
            return;
        }

        // Check if store instance is registered for time-travel
        if (!_stores.TryGetValue(storeName, out var store))
        {
            return;
        }

        var snapshot = history[index];

        _isTimeTraveling = true;
        try
        {
            // Use reflection to call internal SetRestoredState method
            var setMethod = store.GetType().GetMethod(
                "SetRestoredState",
                BindingFlags.Instance | BindingFlags.NonPublic);

            setMethod?.Invoke(store, new[] { snapshot.State });

            // Trigger StateChanged notification on the store
            // Note: NonPublic flag covers both private and protected members
            var onChangedMethod = store.GetType().GetMethod(
                "OnStateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

            onChangedMethod?.Invoke(store, null);
        }
        finally
        {
            _isTimeTraveling = false;
        }

        _currentIndex[storeName] = index;

        // Notify subscribers
        StateHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Registers a store instance for time-travel operations.
    /// </summary>
    /// <param name="storeName">The name of the store.</param>
    /// <param name="store">The store instance.</param>
    /// <remarks>
    /// This method should be called during middleware setup to enable
    /// time-travel functionality for the store.
    /// </remarks>
    internal void RegisterStore(string storeName, IStore store)
    {
        _stores[storeName] = store;
    }
}
