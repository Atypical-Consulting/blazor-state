using Bustand.Persistence;
using Microsoft.JSInterop;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Bustand.Tests.Persistence;

public class BrowserStorageServiceTests
{
    private record TestState(int Value, string? Name = null);

    [Fact]
    public async Task GetAsync_WhenNotAvailable_ReturnsDefault()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        // Note: service is not available by default (SetAvailable not called)

        // Act
        var result = await service.GetAsync<TestState>("test-key", StorageType.Local);

        // Assert
        Assert.Null(result);
        await jsRuntime.DidNotReceive().InvokeAsync<string?>(
            Arg.Any<string>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task GetAsync_WhenAvailable_CallsJsRuntime()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<string?>("{\"value\":42}"));

        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        var result = await service.GetAsync<TestState>("test-key", StorageType.Local);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GetAsync_SessionStorage_UsesSessionStorage()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<string?>("{\"value\":99}"));

        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        var result = await service.GetAsync<TestState>("test-key", StorageType.Session);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task GetAsync_InvalidJson_ReturnsDefault()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<string?>("not-valid-json"));

        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        var result = await service.GetAsync<TestState>("test-key", StorageType.Local);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_JsException_ReturnsDefault()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .ThrowsAsync(new JSException("Storage not available"));

        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        var result = await service.GetAsync<TestState>("test-key", StorageType.Local);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WhenNotAvailable_DoesNotCallJsRuntime()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);

        // Act
        await service.SetAsync("test-key", new TestState(1), StorageType.Local);

        // Assert
        await jsRuntime.DidNotReceive().InvokeVoidAsync(
            Arg.Any<string>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task SetAsync_WhenAvailable_CallsJsRuntime()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        await service.SetAsync("test-key", new TestState(42, "Test"), StorageType.Local);

        // Assert
        await jsRuntime.Received(1).InvokeVoidAsync(
            "localStorage.setItem",
            Arg.Is<object?[]?>(args =>
                args != null &&
                args.Length == 2 &&
                args[0]!.ToString() == "test-key"));
    }

    [Fact]
    public async Task RemoveAsync_WhenAvailable_CallsJsRuntime()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();

        // Act
        await service.RemoveAsync("test-key", StorageType.Local);

        // Assert
        await jsRuntime.Received(1).InvokeVoidAsync(
            "localStorage.removeItem",
            Arg.Is<object?[]?>(args =>
                args != null &&
                args.Length == 1 &&
                args[0]!.ToString() == "test-key"));
    }

    [Fact]
    public void IsAvailable_DefaultsFalse()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);

        // Assert
        Assert.False(service.IsAvailable);
    }

    [Fact]
    public void SetAvailable_SetsIsAvailableTrue()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);

        // Act
        service.SetAvailable();

        // Assert
        Assert.True(service.IsAvailable);
    }

    [Fact]
    public void SetAvailable_RaisesOnAvailabilityChanged()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        var eventRaised = false;
        service.OnAvailabilityChanged += () => eventRaised = true;

        // Act
        service.SetAvailable();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetAvailable_DoesNotRaiseEvent_WhenAlreadyAvailable()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable(); // First call

        var eventCount = 0;
        service.OnAvailabilityChanged += () => eventCount++;

        // Act
        service.SetAvailable(); // Second call

        // Assert - should not raise event since already available
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void SetUnavailable_MarksStorageUnavailable()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();
        Assert.True(service.IsAvailable);

        // Act
        service.SetUnavailable();

        // Assert
        Assert.False(service.IsAvailable);
    }

    [Fact]
    public void SetAvailable_AfterSetUnavailable_RaisesEvent()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var service = new BrowserStorageService(jsRuntime);
        service.SetAvailable();
        service.SetUnavailable();

        var eventRaised = false;
        service.OnAvailabilityChanged += () => eventRaised = true;

        // Act
        service.SetAvailable(); // Re-enable

        // Assert - event should fire on re-availability
        Assert.True(eventRaised);
        Assert.True(service.IsAvailable);
    }
}
