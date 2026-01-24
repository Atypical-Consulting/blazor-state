using Bunit;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.DependencyInjection;
using Bustand.Extensions;

namespace Bustand.Tests.Components;

/// <summary>
/// Tests for MODE-06: Prerendering without state mismatch.
/// These tests verify that the component pattern for prerender-safe state works correctly.
/// </summary>
public class PrerenderSafeComponentTests : BunitContext
{
    public PrerenderSafeComponentTests()
    {
        // Register stores for tests
        Services.AddBustand(options =>
            options.ScanAssemblyContaining<CounterStore>());
    }

    [Fact]
    public void Component_CanResolveStore_FromDI()
    {
        // Arrange & Act
        var store = Services.GetRequiredService<CounterStore>();

        // Assert
        Assert.NotNull(store);
        Assert.Equal(0, store.State.Count);
    }

    [Fact]
    public void Store_StateChanges_AreVisibleAcrossComponents()
    {
        // This simulates the prerender-safe pattern where state is shared
        // Arrange
        var store = Services.GetRequiredService<CounterStore>();

        // Act - Simulate state change (as if from prerender)
        store.Increment();
        store.Increment();

        // Assert - State should be visible (simulating hydration sees same state)
        Assert.Equal(2, store.State.Count);

        // Get "another" reference (same instance due to Scoped/Singleton)
        var store2 = Services.GetRequiredService<CounterStore>();
        Assert.Equal(2, store2.State.Count);
        Assert.Same(store, store2); // Same instance
    }

    [Fact]
    public void Store_InitialState_ConsistentAcrossResolutions()
    {
        // MODE-06: Prerendering should not cause state mismatch
        // When a component prerenders and then hydrates, it should see consistent state

        // Arrange - Simulate prerender phase
        var prerenderStore = Services.GetRequiredService<CounterStore>();
        prerenderStore.SetCount(42);

        // Act - Simulate hydration phase (same service scope)
        var hydrateStore = Services.GetRequiredService<CounterStore>();

        // Assert - State should be consistent (no mismatch)
        Assert.Equal(42, hydrateStore.State.Count);
        Assert.Same(prerenderStore, hydrateStore);
    }

    [Fact]
    public void Store_WithEventHandler_DoesNotThrowDuringStateChange()
    {
        // Verify that state change notifications work without throwing
        // (This tests the synchronization context handling from MODE-05)

        // Arrange
        var store = Services.GetRequiredService<CounterStore>();
        var stateChangedCount = 0;
        store.StateChanged += (_, _) => stateChangedCount++;

        // Act - Should not throw even without component context
        store.Increment();
        store.Increment();

        // Assert
        Assert.Equal(2, stateChangedCount);
        Assert.Equal(2, store.State.Count);
    }

    [Fact]
    public void Store_MultipleSubscribers_AllNotified()
    {
        // Prerender pattern: multiple components may subscribe to same store

        // Arrange
        var store = Services.GetRequiredService<CounterStore>();
        var subscriber1Count = 0;
        var subscriber2Count = 0;

        store.StateChanged += (_, _) => subscriber1Count++;
        store.StateChanged += (_, _) => subscriber2Count++;

        // Act
        store.Increment();

        // Assert - Both subscribers notified
        Assert.Equal(1, subscriber1Count);
        Assert.Equal(1, subscriber2Count);
    }
}
