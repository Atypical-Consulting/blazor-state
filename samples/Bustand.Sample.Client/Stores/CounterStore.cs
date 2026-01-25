using Bustand.Core;
using Bustand.Persistence;

namespace Bustand.Sample.Client.Stores;

/// <summary>
/// A simple counter store demonstrating basic Bustand patterns.
///
/// Key concepts demonstrated:
/// - Inheriting from ZustandStore&lt;TState&gt;
/// - Defining InitialState
/// - Using Set() with 'with' expressions for immutable updates
/// - Persistence via [Persist] attribute
///
/// ARCHITECTURE NOTE:
/// This store is in the Client project so it can be used by components
/// in ALL render modes (Server, WebAssembly, Auto). The Server project
/// references the Client project, so Server pages can also access this store.
///
/// This is the simplest possible store - perfect for learning the basics.
/// </summary>
[Persist(StorageType.Local, Key = "counter")]
public class CounterStore : ZustandStore<CounterState>
{
    /// <summary>
    /// Define the initial state. This is called once when the store is created.
    /// If persistence is enabled and data exists, it will be restored after this.
    /// </summary>
    protected override CounterState InitialState => new(Count: 0);

    /// <summary>
    /// Increment the counter by 1.
    ///
    /// Notice: We use Set() with a lambda that receives the current state.
    /// The 'with' expression creates a new record with the updated value.
    /// This pattern ensures immutability - we never modify state directly.
    /// </summary>
    public void Increment()
    {
        // Set() accepts a function: currentState => newState
        // The 'with' expression creates a copy with modified properties
        Set(state => state with { Count = state.Count + 1 });
    }

    /// <summary>
    /// Decrement the counter by 1.
    /// </summary>
    public void Decrement()
    {
        Set(state => state with { Count = state.Count - 1 });
    }

    /// <summary>
    /// Reset the counter to zero.
    ///
    /// Alternative pattern: Set() also accepts a direct state value.
    /// Use this when you don't need the previous state.
    /// </summary>
    public void Reset()
    {
        // Direct value instead of function - useful for complete reset
        Set(new CounterState(Count: 0));
    }

    /// <summary>
    /// Increment by a specific amount.
    /// Shows how to pass parameters to store methods.
    /// </summary>
    public void IncrementBy(int amount)
    {
        Set(state => state with { Count = state.Count + amount });
    }
}
