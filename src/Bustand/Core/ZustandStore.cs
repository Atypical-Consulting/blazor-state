namespace Bustand.Core;

/// <summary>
/// Abstract base class for Bustand stores. Inherit from this class to create a store.
/// </summary>
/// <typeparam name="TState">The state type. Use a C# record for immutability with 'with' expressions.</typeparam>
/// <example>
/// <code>
/// public record CounterState(int Count = 0);
///
/// [BustandStore]
/// public class CounterStore : ZustandStore&lt;CounterState&gt;
/// {
///     public CounterStore() : base(new CounterState()) { }
///
///     public void Increment() => Set(s => s with { Count = s.Count + 1 });
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>
/// <b>Thread Safety (MODE-05):</b> State updates are thread-safe via locking.
/// When subscribing to StateChanged in Blazor Server components, always use
/// <c>InvokeAsync(StateHasChanged)</c> in your event handler for proper
/// synchronization context handling.
/// </para>
/// <para>
/// <b>Example subscription pattern:</b>
/// <code>
/// private async void OnStateChanged(object? sender, EventArgs e)
/// {
///     await InvokeAsync(StateHasChanged);
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class ZustandStore<TState> : IStore<TState> where TState : class
{
    private TState _state;
    private readonly object _lock = new();

    /// <inheritdoc />
    public TState State => _state;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <summary>
    /// Creates a new store with the specified initial state.
    /// </summary>
    /// <param name="initialState">The initial state. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when initialState is null.</exception>
    protected ZustandStore(TState initialState)
    {
        _state = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    /// <summary>
    /// Updates the state using the provided mutator function.
    /// The mutator receives the current state and should return a new state (use 'with' expression for records).
    /// </summary>
    /// <param name="mutator">A function that takes current state and returns new state.</param>
    /// <example>
    /// <code>
    /// // For record state:
    /// Set(state => state with { Count = state.Count + 1 });
    /// </code>
    /// </example>
    protected void Set(Func<TState, TState> mutator)
    {
        lock (_lock)
        {
            _state = mutator(_state);
        }
        OnStateChanged();
    }

    /// <summary>
    /// Raises the StateChanged event to notify subscribers of state changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called automatically after each <see cref="Set"/> call.
    /// Override to add custom behavior such as logging or validation.
    /// </para>
    /// <para>
    /// <b>Important for Blazor Server (MODE-05):</b> Subscribers must use
    /// <c>InvokeAsync(StateHasChanged)</c> in their event handlers to ensure
    /// proper synchronization context handling.
    /// </para>
    /// </remarks>
    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
