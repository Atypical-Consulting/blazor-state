using Bustand.Tests.TestStores;

namespace Bustand.Tests.Core;

public class ZustandStoreTests
{
    [Fact]
    public void Constructor_InitializesWithProvidedState()
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
}
