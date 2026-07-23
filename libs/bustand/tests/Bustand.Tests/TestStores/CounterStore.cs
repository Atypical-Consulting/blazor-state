using Bustand.Attributes;
using Bustand.Core;

namespace Bustand.Tests.TestStores;

[BustandStore]
public class CounterStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new CounterState();

    public void Increment() => Set(s => s with { Count = s.Count + 1 });

    public void Decrement() => Set(s => s with { Count = s.Count - 1 });

    public void SetCount(int count) => Set(s => s with { Count = count });

    /// <summary>
    /// Sets the count using direct state replacement (tests Set(TState) overload).
    /// </summary>
    public void SetCountDirect(int count) => Set(new CounterState(count));

    /// <summary>
    /// Computed property: true if count is greater than 0.
    /// </summary>
    public bool IsPositive => State.Count > 0;

    /// <summary>
    /// Async increment method (tests SetAsync).
    /// </summary>
    public async Task IncrementAsync() => await SetAsync(s => s with { Count = s.Count + 1 });
}
