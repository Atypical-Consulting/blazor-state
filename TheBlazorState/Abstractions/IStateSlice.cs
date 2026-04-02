namespace TheBlazorState.Abstractions;

/// <summary>
/// A reactive wrapper around a single piece of persistent state.
/// Tracks whether the value was restored from prerender or freshly initialized,
/// notifies subscribers on change, and supports staleness detection.
/// </summary>
public interface IStateSlice<T> : IDisposable
{
    /// <summary>Current value of the state slice.</summary>
    T Value { get; set; }

    /// <summary>True if the value was restored from prerendered state.</summary>
    bool WasRestored { get; }

    /// <summary>True if the value has been modified since restore/init.</summary>
    bool IsDirty { get; }

    /// <summary>True if the state has exceeded its configured TTL.</summary>
    bool IsStale { get; }

    /// <summary>UTC timestamp when the value was last set or restored.</summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>Fires whenever <see cref="Value"/> changes.</summary>
    event Action? OnChanged;

    /// <summary>
    /// Sets the value only if it was not restored from prerender.
    /// Returns true if the value was actually set (i.e., first initialization).
    /// This replaces the manual <c>if (value is null)</c> pattern.
    /// </summary>
    bool InitializeIfNeeded(T value);

    /// <summary>
    /// Async version: calls the factory only when no restored value exists.
    /// Avoids invoking expensive async calls when state was already persisted.
    /// </summary>
    Task<bool> InitializeIfNeededAsync(Func<Task<T>> factory);
}
