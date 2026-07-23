using Bustand.Middleware;
using Bustand.Persistence;
using NSubstitute;

namespace Bustand.Tests.Persistence;

public class PersistenceMiddlewareTests
{
    private record TestState(int Value);

    [Fact]
    public void OnBeforeChange_AlwaysReturnsTrue()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local);

        var context = new MiddlewareContext<TestState>
        {
            OldState = new TestState(1),
            NewState = new TestState(2),
            StoreType = typeof(object),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = middleware.OnBeforeChange(context);

        // Assert
        Assert.True(result);

        middleware.Dispose();
    }

    [Fact]
    public void OnAfterChange_QueuesWriteToStorage()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        storage.IsAvailable.Returns(true);

        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local, debounceMs: 50);

        var context = new MiddlewareContext<TestState>
        {
            OldState = new TestState(1),
            NewState = new TestState(42),
            StoreType = typeof(object),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        middleware.OnAfterChange(context);

        // Wait for debounce
        Thread.Sleep(100);

        // Assert
        storage.Received(1).SetAsync("test-key", Arg.Is<TestState>(s => s.Value == 42), StorageType.Local);

        middleware.Dispose();
    }

    [Fact]
    public async Task RestoreStateAsync_WhenAvailable_ReturnsState()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        storage.IsAvailable.Returns(true);
        storage.GetAsync<TestState>("test-key", StorageType.Local)
            .Returns(Task.FromResult<TestState?>(new TestState(99)));

        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local);

        // Act
        var result = await middleware.RestoreStateAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99, result.Value);

        middleware.Dispose();
    }

    [Fact]
    public async Task RestoreStateAsync_WhenNotAvailable_ReturnsNull()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        storage.IsAvailable.Returns(false);

        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local);

        // Act
        var result = await middleware.RestoreStateAsync();

        // Assert
        Assert.Null(result);

        middleware.Dispose();
    }

    [Fact]
    public async Task FlushAsync_ForcesImmediateWrite()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        storage.IsAvailable.Returns(true);

        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local, debounceMs: 10000); // Long debounce

        var context = new MiddlewareContext<TestState>
        {
            OldState = new TestState(1),
            NewState = new TestState(42),
            StoreType = typeof(object),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        middleware.OnAfterChange(context);
        await middleware.FlushAsync(); // Force immediate

        // Assert - should be written immediately
        await storage.Received(1).SetAsync("test-key", Arg.Is<TestState>(s => s.Value == 42), StorageType.Local);

        middleware.Dispose();
    }

    [Fact]
    public void StorageKey_ReturnsConfiguredKey()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        var middleware = new PersistenceMiddleware<TestState>(
            storage, "my-custom-key", StorageType.Session);

        // Assert
        Assert.Equal("my-custom-key", middleware.StorageKey);
        Assert.Equal(StorageType.Session, middleware.StorageType);

        middleware.Dispose();
    }

    [Fact]
    public void Dispose_PreventsSubsequentWrites()
    {
        // Arrange
        var storage = Substitute.For<IBrowserStorage>();
        storage.IsAvailable.Returns(true);

        var middleware = new PersistenceMiddleware<TestState>(
            storage, "test-key", StorageType.Local, debounceMs: 50);

        var context = new MiddlewareContext<TestState>
        {
            OldState = new TestState(1),
            NewState = new TestState(42),
            StoreType = typeof(object),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        middleware.Dispose();
        middleware.OnAfterChange(context);

        Thread.Sleep(100);

        // Assert - should not write after dispose
        storage.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<TestState>(), Arg.Any<StorageType>());
    }
}
