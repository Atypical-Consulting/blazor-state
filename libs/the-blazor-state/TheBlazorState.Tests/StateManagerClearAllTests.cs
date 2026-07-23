using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TheBlazorState.Abstractions;
using TheBlazorState.Configuration;
using TheBlazorState.Extensions;
using TheBlazorState.Services;
using TheBlazorState.Storage;
using Xunit;
using static TheBlazorState.Services.StateManager;

namespace TheBlazorState.Tests;

public class StateManagerClearAllTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly IMemoryCache _cache;

    public StateManagerClearAllTests()
    {
        _ctx = new BunitContext();
        _ctx.AddBunitPersistentComponentState();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _ctx.Services.AddSingleton(_cache);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _ctx.Dispose();
    }

    private StateManager CreateManager()
    {
        var options = new TheBlazorStateOptions();
        var browserStorage = new BrowserStorageService(null!);
        var initializer = new StorageStrategyInitializer(browserStorage, _cache);
        var crossTabSync = new CrossTabSyncService(null!);
        var hub = new CrossTabHub();
        return new StateManager(
            _ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.PersistentComponentState>(),
            _cache,
            NullLogger<StateManager>.Instance,
            options,
            initializer,
            crossTabSync,
            hub);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesKeysFromServerCache()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int counter = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.ServerMemoryCache(),
            meta,
            v => counter = v,
            () => counter);

        // Set a value so it's cached
        counter = 42;
        meta.MarkDirty();
        meta.RaiseChanged();

        _cache.TryGetValue<PersistedEnvelope<int>>("Test.Counter", out _).ShouldBeTrue();

        // Act
        await manager.ClearAllAsync();

        // Assert
        _cache.TryGetValue<PersistedEnvelope<int>>("Test.Counter", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ClearAllAsync_RemovesMultipleKeys()
    {
        var manager = CreateManager();
        var meta1 = new StateMeta(ttl: null);
        var meta2 = new StateMeta(ttl: null);
        int counter = 0;
        string color = "red";

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.ServerMemoryCache(),
            meta1,
            v => counter = v,
            () => counter);

        manager.RestoreProperty(
            "Test.Color",
            StorageStrategy.ServerMemoryCache(),
            meta2,
            v => color = v,
            () => color);

        // Set values so they're cached
        counter = 42;
        meta1.MarkDirty();
        meta1.RaiseChanged();

        color = "blue";
        meta2.MarkDirty();
        meta2.RaiseChanged();

        _cache.TryGetValue("Test.Counter", out _).ShouldBeTrue();
        _cache.TryGetValue("Test.Color", out _).ShouldBeTrue();

        // Act
        await manager.ClearAllAsync();

        // Assert
        _cache.TryGetValue("Test.Counter", out _).ShouldBeFalse();
        _cache.TryGetValue("Test.Color", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ClearAllAsync_AfterDispose_Throws()
    {
        var manager = CreateManager();
        manager.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(() => manager.ClearAllAsync());
    }

    [Fact]
    public async Task ClearAllAsync_WithNoKeys_DoesNotThrow()
    {
        var manager = CreateManager();

        await Should.NotThrowAsync(() => manager.ClearAllAsync());
    }

    [Fact]
    public async Task ClearAllAsync_CacheMiss_OnSubsequentRestore()
    {
        // End-to-end: register, set value, clear, then a new manager
        // should NOT find the value in cache.
        var manager1 = CreateManager();
        var meta1 = new StateMeta(ttl: null);
        int value1 = 0;

        manager1.RestoreProperty(
            "Test.Counter",
            StorageStrategy.ServerMemoryCache(),
            meta1,
            v => value1 = v,
            () => value1);

        value1 = 99;
        meta1.MarkDirty();
        meta1.RaiseChanged();

        await manager1.ClearAllAsync();
        manager1.Dispose();

        // New manager (simulates new circuit after force reload)
        var manager2 = CreateManager();
        var meta2 = new StateMeta(ttl: null);
        int value2 = 0;

        manager2.RestoreProperty(
            "Test.Counter",
            StorageStrategy.ServerMemoryCache(),
            meta2,
            v => value2 = v,
            () => value2);

        // Assert: value was NOT restored from cache
        value2.ShouldBe(0);
        meta2.WasRestored.ShouldBeFalse();

        manager2.Dispose();
    }
}
