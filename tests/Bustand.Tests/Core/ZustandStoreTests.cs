using Bustand.Core;
using Bustand.Tests.TestStores;

namespace Bustand.Tests.Core;

public class ZustandStoreTests
{
    [Fact]
    public void InitialState_IsUsedWhenStoreCreated()
    {
        // Arrange & Act
        var store = new CounterStore();

        // Assert
        Assert.Equal(0, store.State.Count);
    }

    [Fact]
    public void Set_UpdatesStateCorrectly()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        store.Increment();

        // Assert
        Assert.Equal(1, store.State.Count);
    }

    [Fact]
    public void Set_MultipleUpdates_AccumulateCorrectly()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        store.Increment();
        store.Increment();
        store.Increment();

        // Assert
        Assert.Equal(3, store.State.Count);
    }

    [Fact]
    public void Set_WithCustomValue_SetsCorrectly()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        store.SetCount(42);

        // Assert
        Assert.Equal(42, store.State.Count);
    }

    [Fact]
    public void Set_DirectReplacement_SetsCorrectly()
    {
        // Arrange
        var store = new CounterStore();

        // Act - Test Set(TState) overload
        store.SetCountDirect(99);

        // Assert
        Assert.Equal(99, store.State.Count);
    }

    [Fact]
    public void StateChanged_RaisesEventOnUpdate()
    {
        // Arrange
        var store = new CounterStore();
        var eventRaised = false;
        store.StateChanged += (_, _) => eventRaised = true;

        // Act
        store.Increment();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void StateChanged_RaisesEventForEachUpdate()
    {
        // Arrange
        var store = new CounterStore();
        var eventCount = 0;
        store.StateChanged += (_, _) => eventCount++;

        // Act
        store.Increment();
        store.Increment();
        store.Decrement();

        // Assert
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void State_IsImmutable_NewInstanceOnUpdate()
    {
        // Arrange
        var store = new CounterStore();
        var originalState = store.State;

        // Act
        store.Increment();

        // Assert
        Assert.NotSame(originalState, store.State);
        Assert.Equal(0, originalState.Count); // Original unchanged
        Assert.Equal(1, store.State.Count);   // New state has update
    }

    [Fact]
    public void BeginRender_EndRender_AllowsSetOutsideRender()
    {
        // Arrange
        var store = new CounterStore();

        // Act - Simulate render cycle ending
        store.BeginRender();
        store.EndRender();
        store.Increment(); // Should work after render ends

        // Assert
        Assert.Equal(1, store.State.Count);
    }

    [Fact]
    public void Set_DuringRender_ThrowsRenderLoopException()
    {
        // Arrange
        var store = new TestRenderLoopStore();
        store.BeginRender();

        // Act & Assert
        var exception = Assert.Throws<RenderLoopException>(() => store.TryIncrement());
        Assert.Equal(typeof(TestRenderLoopStore), exception.StoreType);
        Assert.Contains("TestRenderLoopStore", exception.Message);
        Assert.Contains("infinite render loop", exception.Message);

        // Cleanup
        store.EndRender();
    }

    [Fact]
    public void IsInitialized_IsFalseBeforeInitializeAsync()
    {
        // Arrange & Act
        var store = new CounterStore();

        // Assert - stores without async init are not "initialized" until EnsureInitializedAsync is called
        Assert.False(store.IsInitialized);
    }

    [Fact]
    public async Task EnsureInitializedAsync_SetsIsInitializedToTrue()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        await store.EnsureInitializedAsync();

        // Assert
        Assert.True(store.IsInitialized);
    }

    [Fact]
    public async Task EnsureInitializedAsync_IsIdempotent()
    {
        // Arrange
        var store = new CounterStore();

        // Act - Call multiple times
        await store.EnsureInitializedAsync();
        await store.EnsureInitializedAsync();
        await store.EnsureInitializedAsync();

        // Assert - Should not throw, should remain initialized
        Assert.True(store.IsInitialized);
    }
}

/// <summary>
/// Test store that exposes Set for render loop testing.
/// </summary>
public class TestRenderLoopStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new CounterState();

    public void TryIncrement() => Set(s => s with { Count = s.Count + 1 });
}
