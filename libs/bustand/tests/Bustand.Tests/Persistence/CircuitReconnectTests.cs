using Bustand.Blazor;
using Bustand.Persistence;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.JSInterop;
using NSubstitute;

namespace Bustand.Tests.Persistence;

/// <summary>
/// Tests for circuit reconnect state restoration (PERS-04).
/// Verifies that persisted state is properly restored when a Blazor Server circuit reconnects.
/// </summary>
/// <remarks>
/// These tests focus on the IBrowserStorage interface behavior during circuit lifecycle events.
/// The BustandCircuitHandler is tested indirectly through its effects on storage availability,
/// since Circuit is a sealed class that's difficult to mock.
/// </remarks>
public class CircuitReconnectTests
{
    private record TestState(int Value);

    [Fact]
    public void Storage_SetAvailable_MarksStorageAvailable()
    {
        // Arrange - simulates OnConnectionUp behavior
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);
        Assert.False(storage.IsAvailable);

        // Act - this is what OnConnectionUp calls
        storage.SetAvailable();

        // Assert
        Assert.True(storage.IsAvailable);
    }

    [Fact]
    public void Storage_SetUnavailable_MarksStorageUnavailable()
    {
        // Arrange - simulates OnConnectionDown behavior
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);
        storage.SetAvailable();
        Assert.True(storage.IsAvailable);

        // Act - this is what OnConnectionDown calls
        storage.SetUnavailable();

        // Assert
        Assert.False(storage.IsAvailable);
    }

    [Fact]
    public void CircuitReconnect_RaisesOnAvailabilityChanged()
    {
        // Arrange - simulates circuit disconnect then reconnect
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);
        storage.SetAvailable(); // Initial availability

        // Simulate disconnect
        storage.SetUnavailable();
        Assert.False(storage.IsAvailable);

        var eventRaised = false;
        storage.OnAvailabilityChanged += () => eventRaised = true;

        // Act - simulate reconnect
        storage.SetAvailable();

        // Assert
        Assert.True(eventRaised);
        Assert.True(storage.IsAvailable);
    }

    [Fact]
    public void CircuitLifecycle_FullCycle_HandlesCorrectly()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);
        var availabilityChanges = new List<bool>();

        storage.OnAvailabilityChanged += () => availabilityChanges.Add(storage.IsAvailable);

        // Act - full circuit lifecycle
        // 1. Circuit opens (storage not available yet - needs first render)
        Assert.False(storage.IsAvailable); // Storage requires first render

        // 2. First render happens (BustandInitializer calls SetAvailable)
        storage.SetAvailable();
        Assert.True(storage.IsAvailable);

        // 3. Connection drops (OnConnectionDown calls SetUnavailable)
        storage.SetUnavailable();
        Assert.False(storage.IsAvailable);

        // 4. Connection reconnects (OnConnectionUp calls SetAvailable)
        storage.SetAvailable();
        Assert.True(storage.IsAvailable);

        // 5. Circuit closes (OnCircuitClosed calls SetUnavailable)
        storage.SetUnavailable();
        Assert.False(storage.IsAvailable);

        // Assert - availability changed events
        // Events: SetAvailable(true), SetAvailable(true after reconnect) = 2 true events
        Assert.Contains(true, availabilityChanges);
    }

    [Fact]
    public void CircuitHandler_WithMockedStorage_WorksWithInterface()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        var handler = new BustandCircuitHandler(storage);

        // Act & Assert - should not throw
        Assert.NotNull(handler);
    }

    [Fact]
    public void CircuitHandler_NullStorage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BustandCircuitHandler(null!));
    }

    [Fact]
    public void CircuitOpened_DoesNotMarkAvailable()
    {
        // Arrange - circuit opening doesn't mean storage is ready
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);

        // Assert - storage should NOT be available until first render
        Assert.False(storage.IsAvailable);
    }

    [Fact]
    public void MultipleSetAvailable_OnlyRaisesEventOnce()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);

        var eventCount = 0;
        storage.OnAvailabilityChanged += () => eventCount++;

        // Act - multiple SetAvailable calls
        storage.SetAvailable();
        storage.SetAvailable();
        storage.SetAvailable();

        // Assert - event should only fire once (on first availability)
        Assert.Equal(1, eventCount);
        Assert.True(storage.IsAvailable);
    }

    [Fact]
    public void ReconnectCycle_EventFiringPattern()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);

        var eventCount = 0;
        storage.OnAvailabilityChanged += () => eventCount++;

        // Act - simulate: initial availability -> disconnect -> reconnect -> disconnect -> reconnect
        storage.SetAvailable(); // First availability - event fires
        Assert.Equal(1, eventCount);

        storage.SetUnavailable(); // Disconnect - no event (only fires on availability)
        Assert.Equal(1, eventCount);

        storage.SetAvailable(); // Reconnect - event fires
        Assert.Equal(2, eventCount);

        storage.SetUnavailable(); // Disconnect - no event
        Assert.Equal(2, eventCount);

        storage.SetAvailable(); // Reconnect again - event fires
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public async Task Storage_WhenUnavailable_OperationsAreNoOp()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var storage = new BrowserStorageService(jsRuntime);
        // Not calling SetAvailable - storage is unavailable

        // Act - operations should be no-op when unavailable
        var result = await storage.GetAsync<TestState>("key", StorageType.Local);
        await storage.SetAsync("key", new TestState(42), StorageType.Local);
        await storage.RemoveAsync("key", StorageType.Local);

        // Assert - no JS calls should have been made
        Assert.Null(result);
        await jsRuntime.DidNotReceive().InvokeAsync<string?>(
            Arg.Any<string>(), Arg.Any<object?[]?>());
        await jsRuntime.DidNotReceive().InvokeVoidAsync(
            Arg.Any<string>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task Storage_AfterReconnect_OperationsWork()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<string?>("{\"value\":42}"));

        var storage = new BrowserStorageService(jsRuntime);

        // Simulate: initial available -> disconnect -> reconnect
        storage.SetAvailable();
        storage.SetUnavailable();
        storage.SetAvailable(); // Reconnected

        // Act - operations should work after reconnect
        var result = await storage.GetAsync<TestState>("key", StorageType.Local);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
    }

    // Note: BustandCircuitHandler lifecycle methods are tested indirectly through
    // the IBrowserStorage interface behavior since Circuit is a sealed class with
    // internal constructors that cannot be mocked or instantiated directly.
    // The behavior is verified through the storage availability tests above.

    [Fact]
    public void CircuitHandler_IsCircuitHandler_ImplementsBaseClass()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        var handler = new BustandCircuitHandler(storage);

        // Assert - verify handler is a proper CircuitHandler
        Assert.IsAssignableFrom<CircuitHandler>(handler);
    }

    [Fact]
    public void CircuitHandler_CanBeCreated_WithValidStorage()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();

        // Act
        var handler = new BustandCircuitHandler(storage);

        // Assert
        Assert.NotNull(handler);
    }
}
