using Bustand.Core;
using Bustand.DevTools.Models;
using Bustand.DevTools.Services;
using Xunit;

namespace Bustand.Tests.DevTools;

/// <summary>
/// Unit tests for <see cref="DevToolsStore"/> covering history management and time-travel.
/// </summary>
public class DevToolsStoreTests
{
    private readonly DevToolsStore _store;

    public DevToolsStoreTests()
    {
        _store = new DevToolsStore();
    }

    [Fact]
    public void RecordStateChange_AddsToHistory()
    {
        // Arrange
        var oldState = new TestState("initial");
        var newState = new TestState("updated");

        // Act
        _store.RecordStateChange(typeof(TestStore), oldState, newState, "Update", DateTimeOffset.UtcNow);

        // Assert
        var history = _store.GetHistory("TestStore");
        Assert.Single(history);
        Assert.Equal(newState, history[0].State);
    }

    [Fact]
    public void RecordStateChange_EnforcesMaxHistoryLimit()
    {
        // Arrange - add 101 entries
        for (int i = 0; i <= 100; i++)
        {
            _store.RecordStateChange(
                typeof(TestStore),
                new TestState($"state-{i}"),
                new TestState($"state-{i + 1}"),
                $"Action-{i}",
                DateTimeOffset.UtcNow);
        }

        // Assert - should have max 100 entries
        var history = _store.GetHistory("TestStore");
        Assert.Equal(100, history.Count);

        // First entry should be state-2 (oldest removed)
        Assert.Equal("state-2", ((TestState)history[0].State).Value);
    }

    [Fact]
    public void GetCurrentSnapshot_ReturnsLatest()
    {
        // Arrange
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act1", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStore), new TestState("b"), new TestState("c"), "Act2", DateTimeOffset.UtcNow);

        // Act
        var current = _store.GetCurrentSnapshot("TestStore");

        // Assert
        Assert.NotNull(current);
        Assert.Equal("c", ((TestState)current.State).Value);
    }

    [Fact]
    public void GetCurrentSnapshot_ReturnsNull_WhenNoHistory()
    {
        // Act
        var current = _store.GetCurrentSnapshot("NonExistentStore");

        // Assert
        Assert.Null(current);
    }

    [Fact]
    public void RegisteredStoreNames_TracksAllStores()
    {
        // Arrange
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(OtherStore), new OtherState(1), new OtherState(2), "Act", DateTimeOffset.UtcNow);

        // Act
        var names = _store.RegisteredStoreNames;

        // Assert
        Assert.Contains("TestStore", names);
        Assert.Contains("OtherStore", names);
    }

    [Fact]
    public void RegisteredStoreNames_IsEmpty_Initially()
    {
        // Act
        var names = _store.RegisteredStoreNames;

        // Assert
        Assert.Empty(names);
    }

    [Fact]
    public void StateHistoryChanged_FiresOnRecord()
    {
        // Arrange
        var eventFired = false;
        _store.StateHistoryChanged += (_, _) => eventFired = true;

        // Act
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act", DateTimeOffset.UtcNow);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void IsTimeTraveling_IsFalse_Initially()
    {
        // Assert
        Assert.False(_store.IsTimeTraveling);
    }

    [Fact]
    public void GetCurrentIndex_ReturnsCorrectPosition()
    {
        // Arrange
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act1", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStore), new TestState("b"), new TestState("c"), "Act2", DateTimeOffset.UtcNow);

        // Act
        var index = _store.GetCurrentIndex("TestStore");

        // Assert
        Assert.Equal(1, index); // 0-based, second entry
    }

    [Fact]
    public void GetCurrentIndex_ReturnsNegativeOne_WhenNoHistory()
    {
        // Act
        var index = _store.GetCurrentIndex("NonExistentStore");

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void GetHistory_ReturnsEmptyList_WhenNoHistory()
    {
        // Act
        var history = _store.GetHistory("NonExistentStore");

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public void RecordStateChange_SerializesStateToJson()
    {
        // Arrange
        var newState = new TestState("test-value");

        // Act
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), newState, "Act", DateTimeOffset.UtcNow);

        // Assert
        var history = _store.GetHistory("TestStore");
        Assert.Single(history);
        Assert.Contains("test-value", history[0].StateJson);
    }

    [Fact]
    public void RecordStateChange_IncludesActionNameInSnapshot()
    {
        // Arrange
        var actionName = "CustomAction";

        // Act
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), actionName, DateTimeOffset.UtcNow);

        // Assert
        var history = _store.GetHistory("TestStore");
        Assert.Equal(actionName, history[0].ActionName);
    }

    [Fact]
    public void RecordStateChange_IncludesTimestampInSnapshot()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act", timestamp);

        // Assert
        var history = _store.GetHistory("TestStore");
        Assert.Equal(timestamp, history[0].Timestamp);
    }

    [Fact]
    public void JumpToState_DoesNothing_WhenStoreNotRegistered()
    {
        // Arrange
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act", DateTimeOffset.UtcNow);

        // Act - no exception, but no state change since store isn't registered
        _store.JumpToState("TestStore", 0);

        // Assert - index should not change since store isn't registered for time-travel
        var index = _store.GetCurrentIndex("TestStore");
        Assert.Equal(0, index);
    }

    [Fact]
    public void JumpToState_DoesNothing_WhenStoreDoesNotExist()
    {
        // Act - should not throw
        _store.JumpToState("NonExistentStore", 0);

        // Assert - no exception
        Assert.True(true);
    }

    [Fact]
    public void JumpToState_DoesNothing_WhenIndexOutOfBounds()
    {
        // Arrange
        _store.RecordStateChange(typeof(TestStore), new TestState("a"), new TestState("b"), "Act", DateTimeOffset.UtcNow);

        // Act - should not throw
        _store.JumpToState("TestStore", 100);
        _store.JumpToState("TestStore", -1);

        // Assert - no exception
        var index = _store.GetCurrentIndex("TestStore");
        Assert.Equal(0, index);
    }

    [Fact]
    public void JumpToState_WithRegisteredStore_UpdatesCurrentIndex()
    {
        // Arrange
        var testStore = new TestStoreWithState();
        _store.RegisterStore("TestStoreWithState", testStore);

        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("a"), new TestState("b"), "Act1", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("b"), new TestState("c"), "Act2", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("c"), new TestState("d"), "Act3", DateTimeOffset.UtcNow);

        // Act
        _store.JumpToState("TestStoreWithState", 1);

        // Assert
        var index = _store.GetCurrentIndex("TestStoreWithState");
        Assert.Equal(1, index);
    }

    [Fact]
    public void JumpToState_FiresStateHistoryChanged()
    {
        // Arrange
        var testStore = new TestStoreWithState();
        _store.RegisterStore("TestStoreWithState", testStore);

        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("a"), new TestState("b"), "Act1", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("b"), new TestState("c"), "Act2", DateTimeOffset.UtcNow);

        var eventFired = false;
        _store.StateHistoryChanged += (_, _) => eventFired = true;

        // Act
        _store.JumpToState("TestStoreWithState", 0);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void RecordStateChange_TruncatesFuture_WhenBranching()
    {
        // Arrange
        var testStore = new TestStoreWithState();
        _store.RegisterStore("TestStoreWithState", testStore);

        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("a"), new TestState("b"), "Act1", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("b"), new TestState("c"), "Act2", DateTimeOffset.UtcNow);
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("c"), new TestState("d"), "Act3", DateTimeOffset.UtcNow);

        // Jump back to index 0
        _store.JumpToState("TestStoreWithState", 0);

        // Record new state - should branch, removing future entries
        _store.RecordStateChange(typeof(TestStoreWithState), new TestState("b"), new TestState("e"), "Branch", DateTimeOffset.UtcNow);

        // Assert - history should have original entry + new branch entry = 2 entries
        var history = _store.GetHistory("TestStoreWithState");
        Assert.Equal(2, history.Count);
        Assert.Equal("b", ((TestState)history[0].State).Value);
        Assert.Equal("e", ((TestState)history[1].State).Value);
    }

    // Test state and store classes
    private record TestState(string Value);
    private record OtherState(int Number);
    private class TestStore { }
    private class OtherStore { }

    /// <summary>
    /// A minimal store for time-travel testing that inherits from ZustandStore.
    /// </summary>
    private class TestStoreWithState : ZustandStore<TestState>
    {
        protected override TestState InitialState => new("initial");

        public void SetState(TestState state)
        {
            Set(_ => state);
        }
    }
}
