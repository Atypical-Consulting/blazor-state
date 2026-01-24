using Bustand.Core;
using Bustand.Tests.TestStores;

namespace Bustand.Tests.Core;

/// <summary>
/// Enhanced tests covering CORE-01 through CORE-08 requirements.
/// </summary>
public class ZustandStoreEnhancedTests
{
    // CORE-01: Developer can create store by inheriting from ZustandStore<TState>
    [Fact]
    public void Store_InheritsFromZustandStore()
    {
        var store = new CounterStore();
        Assert.IsAssignableFrom<ZustandStore<CounterState>>(store);
    }

    // CORE-02: Developer can define state as C# record
    [Fact]
    public void State_IsRecord_SupportsWithExpression()
    {
        var state = new CounterState(5);
        var newState = state with { Count = 10 };
        Assert.Equal(10, newState.Count);
        Assert.NotSame(state, newState);
    }

    // CORE-03: Set() with direct state replacement
    [Fact]
    public void Set_DirectStateReplacement_UpdatesState()
    {
        var store = new CounterStore();
        store.SetCount(42);
        Assert.Equal(42, store.State.Count);
    }

    // CORE-03: Set() with mutator function
    [Fact]
    public void Set_MutatorFunction_UpdatesState()
    {
        var store = new CounterStore();
        store.Increment();
        store.Increment();
        Assert.Equal(2, store.State.Count);
    }

    // CORE-04: SetAsync for async updates
    [Fact]
    public async Task SetAsync_UpdatesStateAsynchronously()
    {
        var store = new CounterStore();
        await store.IncrementAsync();
        Assert.Equal(1, store.State.Count);
    }

    // CORE-05: InitialState property
    [Fact]
    public void InitialState_ProvidesStartingState()
    {
        var store = new CounterStore();
        Assert.Equal(0, store.State.Count);
    }

    // CORE-06: Derived/computed state
    [Fact]
    public void ComputedState_DerivedFromState()
    {
        var store = new CounterStore();
        store.SetCount(10);
        Assert.True(store.IsPositive);
    }

    [Fact]
    public void ComputedState_UpdatesWithState()
    {
        var store = new CounterStore();
        Assert.False(store.IsPositive);
        store.Increment();
        Assert.True(store.IsPositive);
        store.Decrement();
        Assert.False(store.IsPositive);
    }

    // CORE-07: Store notifies subscribers
    [Fact]
    public void StateChanged_RaisedOnSet()
    {
        var store = new CounterStore();
        var raised = false;
        store.StateChanged += (s, e) => raised = true;
        store.Increment();
        Assert.True(raised);
    }

    // CORE-08: Type-safe updates (compile-time, so just verify signature works)
    [Fact]
    public void Set_TypeSafe_CompileTimeChecked()
    {
        var store = new CounterStore();
        // This would not compile: store.Set("invalid")
        store.SetCount(5);
        Assert.Equal(5, store.State.Count);
    }

    // InitializeAsync tests
    [Fact]
    public async Task InitializeAsync_CalledOnEnsureInitialized()
    {
        var store = new AsyncInitStore(100);
        Assert.False(store.State.Loaded);

        await store.EnsureInitializedAsync();

        Assert.True(store.State.Loaded);
        Assert.Equal(100, store.State.Value);
    }

    [Fact]
    public void IsInitialized_FalseBeforeInit()
    {
        var store = new AsyncInitStore();
        Assert.False(store.IsInitialized);
    }

    [Fact]
    public async Task IsInitialized_TrueAfterInit()
    {
        var store = new AsyncInitStore();
        await store.EnsureInitializedAsync();
        Assert.True(store.IsInitialized);
    }

    // Render loop detection
    [Fact]
    public void Set_DuringRender_ThrowsRenderLoopException()
    {
        var store = new CounterStore();
        store.BeginRender();
        Assert.Throws<RenderLoopException>(() => store.Increment());
    }

    [Fact]
    public void Set_AfterRender_Succeeds()
    {
        var store = new CounterStore();
        store.BeginRender();
        store.EndRender();
        store.Increment(); // Should not throw
        Assert.Equal(1, store.State.Count);
    }

    // Verify multiple async operations work correctly
    [Fact]
    public async Task SetAsync_MultipleUpdates_AccumulateCorrectly()
    {
        var store = new CounterStore();
        await store.IncrementAsync();
        await store.IncrementAsync();
        await store.IncrementAsync();
        Assert.Equal(3, store.State.Count);
    }
}
