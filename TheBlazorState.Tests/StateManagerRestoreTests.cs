using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

public class StateManagerRestoreTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitPersistentComponentState _persistentState;
    private readonly IMemoryCache _cache;

    public StateManagerRestoreTests()
    {
        _ctx = new BunitContext();
        _persistentState = _ctx.AddBunitPersistentComponentState();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _ctx.Services.AddSingleton(_cache);
        _ctx.Services.AddSingleton<ILogger<StateManager>>(NullLogger<StateManager>.Instance);
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
        var crossTabSync = new CrossTabSyncService(_ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());
        return new StateManager(
            _ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.PersistentComponentState>(),
            _cache,
            NullLogger<StateManager>.Instance,
            options,
            initializer,
            crossTabSync);
    }

    // --- Restore from PersistentComponentState ---

    [Fact]
    public void RestoreProperty_FromPrerender_SetsValue()
    {
        // Arrange: persist an envelope in prerender state
        var envelope = new PersistedEnvelope<int>
        {
            Value = 42,
            PersistedAt = DateTimeOffset.UtcNow.AddSeconds(-5)
        };
        _persistentState.Persist("TestComponent.Count", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int restoredValue = 0;

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert
        restoredValue.ShouldBe(42);
        meta.WasRestored.ShouldBeTrue();
    }

    // --- Restore from IMemoryCache when prerender value absent ---

    [Fact]
    public void RestoreProperty_FromMemoryCache_WhenNoPrerenderValue()
    {
        // Arrange: put value in cache only (no prerender)
        var envelope = new PersistedEnvelope<string>
        {
            Value = "cached-data",
            PersistedAt = DateTimeOffset.UtcNow.AddSeconds(-10)
        };
        _cache.Set("TestComponent.Name", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        string restoredValue = "";

        // Act
        manager.RestoreProperty(
            "TestComponent.Name",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert
        restoredValue.ShouldBe("cached-data");
        meta.WasRestored.ShouldBeTrue();
    }

    // --- TTL expired values are discarded ---

    [Fact]
    public void RestoreProperty_DiscardsTtlExpired_PrerenderValue()
    {
        // Arrange: envelope persisted 2 hours ago, TTL is 1 hour
        var envelope = new PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };
        _persistentState.Persist("TestComponent.Count", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: TimeSpan.FromHours(1));
        int restoredValue = 0;

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert: value should not be restored
        restoredValue.ShouldBe(0);
        meta.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void RestoreProperty_DiscardsTtlExpired_CacheValue()
    {
        // Arrange: cached 2 hours ago, TTL is 1 hour
        var envelope = new PersistedEnvelope<int>
        {
            Value = 77,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };
        _cache.Set("TestComponent.Count", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: TimeSpan.FromHours(1));
        int restoredValue = 0;

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert
        restoredValue.ShouldBe(0);
        meta.WasRestored.ShouldBeFalse();
    }

    // --- Fresh TTL values are restored ---

    [Fact]
    public void RestoreProperty_RestoresFreshTtlValue()
    {
        // Arrange: persisted 5 minutes ago, TTL is 1 hour
        var envelope = new PersistedEnvelope<int>
        {
            Value = 55,
            PersistedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        _persistentState.Persist("TestComponent.Count", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: TimeSpan.FromHours(1));
        int restoredValue = 0;

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert
        restoredValue.ShouldBe(55);
        meta.WasRestored.ShouldBeTrue();
    }

    // --- Default value is used when nothing persisted ---

    [Fact]
    public void RestoreProperty_UsesDefault_WhenNothingPersisted()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int restoredValue = 123; // default already set

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => restoredValue = v,
            () => restoredValue);

        // Assert: value unchanged, not marked as restored
        restoredValue.ShouldBe(123);
        meta.WasRestored.ShouldBeFalse();
    }

    // --- Meta is marked as restored with correct timestamp ---

    [Fact]
    public void RestoreProperty_MetaTimestamp_MatchesPersistedAt()
    {
        var persistedAt = DateTimeOffset.UtcNow.AddMinutes(-15);
        var envelope = new PersistedEnvelope<int>
        {
            Value = 10,
            PersistedAt = persistedAt
        };
        _persistentState.Persist("TestComponent.Count", envelope);

        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int val = 0;

        // Act
        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => val = v,
            () => val);

        // Assert
        meta.WasRestored.ShouldBeTrue();
        meta.LastUpdated.ShouldBe(persistedAt);
    }

    // --- Persist callback serializes current value on OnPersisting ---

    [Fact]
    public void RestoreProperty_RegistersPersistCallback()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int currentValue = 42;

        manager.RestoreProperty(
            "TestComponent.Count",
            null,
            meta,
            v => currentValue = v,
            () => currentValue);

        // Act: trigger OnPersisting
        _persistentState.TriggerOnPersisting();

        // Assert: value should be in cache after persisting
        _cache.TryGetValue<PersistedEnvelope<int>>("TestComponent.Count", out var cached).ShouldBeTrue();
        cached!.Value.ShouldBe(42);
    }

    // --- Duplicate key allowed (component re-mount) ---

    [Fact]
    public void RestoreProperty_DuplicateKey_Allowed_For_Remount()
    {
        var manager = CreateManager();
        var meta1 = new StateMeta(ttl: null);
        var meta2 = new StateMeta(ttl: null);
        int val = 0;

        manager.RestoreProperty("TestComponent.Count", null, meta1, v => val = v, () => val);

        // Should not throw — component may re-mount in the same circuit
        Should.NotThrow(() =>
            manager.RestoreProperty("TestComponent.Count", null, meta2, v => val = v, () => val));
    }

    // --- Null/empty key throws ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RestoreProperty_NullOrEmptyKey_Throws(string? key)
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int val = 0;

        Should.Throw<ArgumentException>(() =>
            manager.RestoreProperty(key!, null, meta, v => val = v, () => val));
    }

    // --- After dispose throws ---

    [Fact]
    public void RestoreProperty_AfterDispose_Throws()
    {
        var manager = CreateManager();
        manager.Dispose();

        var meta = new StateMeta(ttl: null);
        int val = 0;

        Should.Throw<ObjectDisposedException>(() =>
            manager.RestoreProperty("TestComponent.Count", null, meta, v => val = v, () => val));
    }
}
