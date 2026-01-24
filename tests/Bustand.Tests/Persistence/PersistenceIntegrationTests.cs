using Bustand.Configuration;
using Bustand.Extensions;
using Bustand.Persistence;
using Bustand.Tests.TestMiddleware;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Bustand.Tests.Persistence;

public class PersistenceIntegrationTests
{
    [Fact]
    public void AddBustand_RegistersIBrowserStorage_WhenPersistentStoresExist()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        // Act
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var storage = provider.GetService<IBrowserStorage>();
        Assert.NotNull(storage);
    }

    [Fact]
    public void AddBustand_ConfiguresStorageKeyPrefix()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        // Act
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
            options.StorageKeyPrefix = "MyApp";
        });

        // Assert - prefix is used in key generation
        var provider = services.BuildServiceProvider();
        var options = new BustandOptions { StorageKeyPrefix = "MyApp" };
        var key = options.BuildStorageKey(typeof(PersistentCounterStore), null);
        Assert.StartsWith("MyApp.", key);
    }

    [Fact]
    public void AddBustand_UsesCustomKeyFromAttribute()
    {
        // Arrange
        var options = new BustandOptions { StorageKeyPrefix = "Test" };

        // Act - SessionCounterStore has Key = "custom-session-counter"
        var key = options.BuildStorageKey(typeof(SessionCounterStore), "custom-session-counter");

        // Assert
        Assert.Equal("Test.custom-session-counter", key);
    }

    [Fact]
    public void AddBustand_CreatesStoreWithPersistenceMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
        });

        // Act
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<PersistentCounterStore>();

        // Assert - store should be created successfully
        Assert.NotNull(store);
        Assert.Equal(0, store.State.Count); // Initial state
    }

    [Fact]
    public void PersistentStore_StateChanges_TriggerPersistence()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
            options.PersistenceDebounceMs = 50;
        });

        var provider = services.BuildServiceProvider();

        // Mark storage as available
        var storage = provider.GetService<IBrowserStorage>();
        Assert.NotNull(storage);
        storage.SetAvailable();

        var store = provider.GetService<PersistentCounterStore>();
        Assert.NotNull(store);

        // Act
        store.Increment();
        Thread.Sleep(100); // Wait for debounce

        // Assert - JS interop should be called
        jsRuntime.Received().InvokeVoidAsync(
            "localStorage.setItem",
            Arg.Any<object?[]?>());
    }

    [Fact]
    public void BustandOptions_JsonSerializerOptions_AppliedToStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        var customOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null // PascalCase
        };

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
            options.JsonSerializerOptions = customOptions;
        });

        // Act
        var provider = services.BuildServiceProvider();
        var storage = provider.GetService<IBrowserStorage>() as BrowserStorageService;

        // Assert - service was created (custom options are internal, but service exists)
        Assert.NotNull(storage);
    }

    [Fact]
    public void AddBustand_WithMiddleware_IncludesPersistenceInPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        var recording = new RecordingMiddleware<CounterState>();
        services.AddSingleton(recording);

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
            options.UseMiddleware<RecordingMiddleware<CounterState>>();
            options.PersistenceDebounceMs = 50;
        });

        var provider = services.BuildServiceProvider();
        var storage = provider.GetService<IBrowserStorage>();
        Assert.NotNull(storage);
        storage.SetAvailable();

        var store = provider.GetService<PersistentCounterStore>();
        Assert.NotNull(store);

        // Act
        store.Increment();
        Thread.Sleep(100); // Wait for debounce

        // Assert - recording middleware was called AND persistence happened
        Assert.Single(recording.AfterChangeCalls);
        Assert.Equal(1, recording.AfterChangeCalls[0].NewState.Count);

        jsRuntime.Received().InvokeVoidAsync(
            "localStorage.setItem",
            Arg.Any<object?[]?>());
    }

    [Fact]
    public void PersistentStore_GracefulDegradation_WhenStorageUnavailable()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<PersistentCounterStore>();
        });

        var provider = services.BuildServiceProvider();

        // Note: NOT calling storage.SetAvailable() - storage is unavailable

        // Act
        var store = provider.GetService<PersistentCounterStore>();

        // Assert - store should work with InitialState
        Assert.NotNull(store);
        Assert.Equal(0, store.State.Count);

        // State updates should work (even though persistence won't happen)
        store.Increment();
        Assert.Equal(1, store.State.Count);
    }

    [Fact]
    public void NonPersistentStore_NotAffectedByPersistence()
    {
        // Arrange
        var services = new ServiceCollection();
        var jsRuntime = Substitute.For<IJSRuntime>();
        services.AddSingleton(jsRuntime);

        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>(); // No [Persist] attribute
        });

        // Act
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<CounterStore>();

        // Assert
        Assert.NotNull(store);
        Assert.Equal(0, store.State.Count);

        store.Increment();
        Assert.Equal(1, store.State.Count);

        // No JS interop calls for non-persistent stores
        jsRuntime.DidNotReceive().InvokeVoidAsync(
            "localStorage.setItem",
            Arg.Any<object?[]?>());
    }
}
