using Bustand.DevTools.Models;

namespace Bustand.DevTools.Services;

/// <summary>
/// Interface for the DevTools state history management service.
/// Provides state change recording, history access, and time-travel capabilities.
/// </summary>
/// <remarks>
/// <para>
/// <b>Key responsibilities:</b>
/// </para>
/// <list type="bullet">
/// <item>Recording state changes from DevToolsMiddleware.</item>
/// <item>Maintaining per-store history limited to 100 entries.</item>
/// <item>Providing time-travel debugging by jumping to historical states.</item>
/// <item>Notifying subscribers when state history changes.</item>
/// </list>
/// </remarks>
public interface IDevToolsStore
{
    /// <summary>
    /// Event raised when any store's state history is updated.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to receive real-time updates when:
    /// <list type="bullet">
    /// <item>A new state change is recorded.</item>
    /// <item>Time-travel navigation occurs.</item>
    /// <item>A new store is registered.</item>
    /// </list>
    /// </remarks>
    event EventHandler? StateHistoryChanged;

    /// <summary>
    /// Gets the list of all registered store names that have recorded state changes.
    /// </summary>
    /// <remarks>
    /// Store names are derived from the store type's name (e.g., "CounterStore").
    /// The list is updated as new stores record their first state change.
    /// </remarks>
    IReadOnlyList<string> RegisteredStoreNames { get; }

    /// <summary>
    /// Gets whether the DevTools is currently performing time-travel navigation.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, middleware should skip recording state changes
    /// to prevent time-travel operations from polluting the history.
    /// </remarks>
    bool IsTimeTraveling { get; }

    /// <summary>
    /// Gets the complete state history for the specified store.
    /// </summary>
    /// <param name="storeName">The name of the store to get history for.</param>
    /// <returns>
    /// A read-only list of state snapshots in chronological order (oldest first).
    /// Returns an empty list if the store has no recorded history.
    /// </returns>
    /// <remarks>
    /// The history is limited to <c>100</c> entries per store.
    /// When the limit is reached, the oldest entries are removed.
    /// </remarks>
    IReadOnlyList<StateSnapshot> GetHistory(string storeName);

    /// <summary>
    /// Gets the current state snapshot for the specified store.
    /// </summary>
    /// <param name="storeName">The name of the store.</param>
    /// <returns>
    /// The current snapshot based on the time-travel position,
    /// or <c>null</c> if the store has no recorded history.
    /// </returns>
    /// <remarks>
    /// During time-travel, this returns the snapshot at the current navigation position,
    /// not necessarily the most recent snapshot in history.
    /// </remarks>
    StateSnapshot? GetCurrentSnapshot(string storeName);

    /// <summary>
    /// Records a state change from middleware.
    /// </summary>
    /// <param name="storeType">The type of the store where the change occurred.</param>
    /// <param name="oldState">The state before the change.</param>
    /// <param name="newState">The state after the change.</param>
    /// <param name="actionName">The name of the action that triggered the change, or null.</param>
    /// <param name="timestamp">The timestamp when the change occurred.</param>
    /// <remarks>
    /// <para>
    /// This method is called by <c>DevToolsMiddleware</c> for each state change.
    /// </para>
    /// <para>
    /// <b>Behavior:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>If <see cref="IsTimeTraveling"/> is true, the change is ignored.</item>
    /// <item>If time-travel occurred and new changes are made, future history is truncated.</item>
    /// <item>History is limited to 100 entries per store (oldest removed when exceeded).</item>
    /// </list>
    /// </remarks>
    void RecordStateChange(
        Type storeType,
        object oldState,
        object newState,
        string? actionName,
        DateTimeOffset timestamp);

    /// <summary>
    /// Jumps to a specific state in the history for time-travel debugging.
    /// </summary>
    /// <param name="storeName">The name of the store to time-travel.</param>
    /// <param name="index">The history index to jump to (0-based).</param>
    /// <remarks>
    /// <para>
    /// This method restores the store's state to the specified historical snapshot.
    /// The <see cref="IsTimeTraveling"/> flag is set during the operation to prevent
    /// the restored state from being recorded as a new history entry.
    /// </para>
    /// <para>
    /// If new state changes occur after time-travel, the history is branched
    /// (future entries from the previous timeline are discarded).
    /// </para>
    /// </remarks>
    void JumpToState(string storeName, int index);

    /// <summary>
    /// Gets the current time-travel position (index) for the specified store.
    /// </summary>
    /// <param name="storeName">The name of the store.</param>
    /// <returns>
    /// The current index in the history, or -1 if the store has no history.
    /// </returns>
    /// <remarks>
    /// This value changes when <see cref="JumpToState"/> is called or when new state changes
    /// are recorded. It always points to the most recent entry unless time-travel is active.
    /// </remarks>
    int GetCurrentIndex(string storeName);
}
