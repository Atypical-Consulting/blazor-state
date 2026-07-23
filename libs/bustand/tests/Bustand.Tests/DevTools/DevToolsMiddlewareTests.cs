using Bustand.DevTools.Middleware;
using Bustand.DevTools.Services;
using Bustand.Middleware;
using NSubstitute;
using Xunit;

namespace Bustand.Tests.DevTools;

/// <summary>
/// Unit tests for <see cref="DevToolsMiddleware{TState}"/>.
/// </summary>
public class DevToolsMiddlewareTests
{
    [Fact]
    public void OnBeforeChange_AlwaysReturnsTrue()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"));

        // Act
        var result = middleware.OnBeforeChange(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OnBeforeChange_DoesNotRecordState()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"));

        // Act
        middleware.OnBeforeChange(context);

        // Assert - RecordStateChange should NOT be called in OnBeforeChange
        devToolsStore.DidNotReceive().RecordStateChange(
            Arg.Any<Type>(),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void OnAfterChange_RecordsToDevToolsStore()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(false);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var oldState = new TestState("a");
        var newState = new TestState("b");
        var context = CreateContext(oldState, newState);

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.Received(1).RecordStateChange(
            Arg.Is<Type>(t => t == typeof(TestStore)),
            Arg.Is<object>(o => o.Equals(oldState)),
            Arg.Is<object>(o => o.Equals(newState)),
            Arg.Is<string>(s => s == "TestAction"),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void OnAfterChange_SkipsWhenTimeTraveling()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(true);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"));

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.DidNotReceive().RecordStateChange(
            Arg.Any<Type>(),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void OnAfterChange_IncludesCorrectStoreType()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(false);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"));

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.Received(1).RecordStateChange(
            Arg.Is<Type>(t => t == typeof(TestStore)),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void OnAfterChange_IncludesActionName()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(false);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"), "CustomAction");

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.Received(1).RecordStateChange(
            Arg.Any<Type>(),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Is<string?>(s => s == "CustomAction"),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void OnAfterChange_IncludesTimestamp()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(false);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var timestamp = new DateTimeOffset(2026, 1, 24, 12, 0, 0, TimeSpan.Zero);
        var context = CreateContext(new TestState("a"), new TestState("b"), timestamp: timestamp);

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.Received(1).RecordStateChange(
            Arg.Any<Type>(),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(t => t == timestamp));
    }

    [Fact]
    public void Constructor_ThrowsOnNullDevToolsStore()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DevToolsMiddleware<TestState>(null!));
    }

    [Fact]
    public void OnAfterChange_HandlesNullActionName()
    {
        // Arrange
        var devToolsStore = Substitute.For<IDevToolsStore>();
        devToolsStore.IsTimeTraveling.Returns(false);
        var middleware = new DevToolsMiddleware<TestState>(devToolsStore);
        var context = CreateContext(new TestState("a"), new TestState("b"), actionName: null);

        // Act
        middleware.OnAfterChange(context);

        // Assert
        devToolsStore.Received(1).RecordStateChange(
            Arg.Any<Type>(),
            Arg.Any<object>(),
            Arg.Any<object>(),
            Arg.Is<string?>(s => s == null),
            Arg.Any<DateTimeOffset>());
    }

    private static MiddlewareContext<TestState> CreateContext(
        TestState oldState,
        TestState newState,
        string? actionName = "TestAction",
        DateTimeOffset? timestamp = null)
    {
        return new MiddlewareContext<TestState>
        {
            OldState = oldState,
            NewState = newState,
            StoreType = typeof(TestStore),
            ActionName = actionName,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
    }

    private record TestState(string Value);
    private class TestStore { }
}
