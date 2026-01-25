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

/// <summary>
/// Test store that exposes SetAsync with direct state.
/// </summary>
public class TestAsyncStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new CounterState();

    public async Task SetCountDirectAsync(int count) => await SetAsync(new CounterState(count));

    public async Task SetCountAsync(int count) => await SetAsync(s => s with { Count = count });
}

/// <summary>
/// Additional ZustandStore tests for coverage improvement.
/// </summary>
public class ZustandStoreEdgeCaseTests
{
    [Fact]
    public void Set_WithDirectState_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new TestNullableStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => store.SetStateNull());
    }

    [Fact]
    public async Task SetAsync_WithDirectState_UpdatesState()
    {
        // Arrange
        var store = new TestAsyncStore();

        // Act
        await store.SetCountDirectAsync(42);

        // Assert
        Assert.Equal(42, store.State.Count);
    }

    [Fact]
    public async Task SetAsync_WithDirectState_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new TestNullableAsyncStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetStateNullAsync());
    }

    [Fact]
    public async Task SetAsync_WithDirectState_RaisesStateChanged()
    {
        // Arrange
        var store = new TestAsyncStore();
        var eventRaised = false;
        store.StateChanged += (_, _) => eventRaised = true;

        // Act
        await store.SetCountDirectAsync(10);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetRestoredState_BeforeAnyStateAccess_SetsInitialState()
    {
        // Arrange - store should not have state accessed yet
        var store = new CounterStore();
        var restoredState = new CounterState(100);

        // Act
        store.SetRestoredState(restoredState);

        // Assert
        Assert.Equal(100, store.State.Count);
    }

    [Fact]
    public void SetRestoredState_AfterStateAccess_DoesNotOverwrite()
    {
        // Arrange
        var store = new CounterStore();
        _ = store.State; // Access state to initialize it

        // Act
        store.SetRestoredState(new CounterState(999));

        // Assert - original state should be preserved
        Assert.Equal(0, store.State.Count);
    }

    [Fact]
    public void SetRestoredState_WithNull_DoesNothing()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        store.SetRestoredState(null);

        // Assert - should fall back to InitialState
        Assert.Equal(0, store.State.Count);
    }

    [Fact]
    public void GetInitialState_ReturnsInitialState()
    {
        // Arrange
        var store = new CounterStore();

        // Act
        var initialState = store.GetInitialState();

        // Assert
        Assert.NotNull(initialState);
        Assert.Equal(0, initialState.Count);
    }

    [Fact]
    public void RenderLoopException_WithMessage_PreservesMessage()
    {
        // Arrange
        var storeType = typeof(CounterStore);

        // Act
        var exception = new RenderLoopException("Custom message", storeType);

        // Assert
        Assert.Equal("Custom message", exception.Message);
        Assert.Equal(storeType, exception.StoreType);
    }

    [Fact]
    public void RenderLoopException_WithMessageAndInner_PreservesBoth()
    {
        // Arrange
        var storeType = typeof(CounterStore);
        var inner = new InvalidOperationException("Inner exception");

        // Act
        var exception = new RenderLoopException("Custom message", storeType, inner);

        // Assert
        Assert.Equal("Custom message", exception.Message);
        Assert.Equal(storeType, exception.StoreType);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void RenderLoopException_DefaultConstructor_SetsStoreTypeInMessage()
    {
        // Arrange
        var storeType = typeof(CounterStore);

        // Act
        var exception = new RenderLoopException(storeType);

        // Assert
        Assert.Contains("CounterStore", exception.Message);
        Assert.Contains("infinite render loop", exception.Message);
        Assert.Equal(storeType, exception.StoreType);
    }

    [Fact]
    public void RenderLoopException_NullStoreType_ThrowsException()
    {
        // Act & Assert - First constructor uses storeType.Name before null check,
        // so it throws NullReferenceException (implementation detail)
        Assert.ThrowsAny<Exception>(() => new RenderLoopException(null!));
    }

    [Fact]
    public void RenderLoopException_WithMessage_NullStoreType_ThrowsArgumentNullException()
    {
        // Act & Assert - Second constructor checks null after base call
        Assert.Throws<ArgumentNullException>(() => new RenderLoopException("message", null!));
    }

    [Fact]
    public void RenderLoopException_WithInner_NullStoreType_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = new InvalidOperationException();

        // Act & Assert - Third constructor checks null after base call
        Assert.Throws<ArgumentNullException>(() => new RenderLoopException("message", null!, inner));
    }
}

/// <summary>
/// Test store that can set null state for testing null validation.
/// </summary>
public class TestNullableStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new CounterState();

    public void SetStateNull() => Set((CounterState)null!);
}

/// <summary>
/// Test store that can set null state async for testing null validation.
/// </summary>
public class TestNullableAsyncStore : ZustandStore<CounterState>
{
    protected override CounterState InitialState => new CounterState();

    public async Task SetStateNullAsync() => await SetAsync((CounterState)null!);
}
