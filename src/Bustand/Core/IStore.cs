namespace Bustand.Core;

/// <summary>
/// Marker interface for all Bustand stores.
/// Used for type constraints and discovery.
/// </summary>
public interface IStore
{
    /// <summary>
    /// Event raised when state changes.
    /// </summary>
    event EventHandler? StateChanged;
}

/// <summary>
/// Generic store interface with typed state access.
/// </summary>
/// <typeparam name="TState">The state type, should be a record for immutability.</typeparam>
public interface IStore<out TState> : IStore where TState : class
{
    /// <summary>
    /// Gets the current state. Read-only access.
    /// </summary>
    TState State { get; }
}
