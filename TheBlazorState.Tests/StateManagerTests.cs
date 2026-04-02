using TheBlazorState.Abstractions;
using TheBlazorState.Services;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace TheBlazorState.Tests;

public class StateManagerTests : BunitContext
{
    private BunitPersistentComponentState FakeState { get; set; } = null!;
    private IMemoryCache Cache { get; set; } = new MemoryCache(new MemoryCacheOptions());

    public StateManagerTests()
    {
        FakeState = AddBunitPersistentComponentState();
    }

    private StateManager CreateManager()
    {
        var pcs = Services.GetRequiredService<PersistentComponentState>();
        return new StateManager(pcs, Cache, NullLogger<StateManager>.Instance);
    }

    // -- CreateSlice ----------------------------------------------------------

    [Fact]
    public void CreateSlice_NoPersistedState_ReturnsDefault()
    {
        using var manager = CreateManager();

        var slice = manager.CreateSlice<int>("counter", defaultValue: 42);

        slice.Value.ShouldBe(42);
        slice.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void CreateSlice_WithPersistedState_RestoresValue()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("counter");

        slice.Value.ShouldBe(99);
        slice.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void CreateSlice_WithExpiredTTL_FallsBackToDefault()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("counter", defaultValue: 0,
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(30));

        slice.Value.ShouldBe(0);
        slice.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void CreateSlice_WithFreshTTL_RestoresValue()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("counter", defaultValue: 0,
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(30));

        slice.Value.ShouldBe(99);
        slice.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void CreateSlice_PersistsValueOnPersisting()
    {
        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("counter");
        slice.Value = 42;

        FakeState.TriggerOnPersisting();

        var found = FakeState.TryTake<StateManager.PersistedEnvelope<int>>("counter", out var envelope);
        found.ShouldBeTrue();
        envelope!.Value.ShouldBe(42);
    }

    [Fact]
    public void CreateSlice_MultipleSlices_AllPersistedOnSingleCallback()
    {
        using var manager = CreateManager();
        var slice1 = manager.CreateSlice<int>("a");
        var slice2 = manager.CreateSlice<string>("b", defaultValue: "hello");
        slice1.Value = 10;
        slice2.Value = "world";

        FakeState.TriggerOnPersisting();

        FakeState.TryTake<StateManager.PersistedEnvelope<int>>("a", out var env1).ShouldBeTrue();
        FakeState.TryTake<StateManager.PersistedEnvelope<string>>("b", out var env2).ShouldBeTrue();
        env1!.Value.ShouldBe(10);
        env2!.Value.ShouldBe("world");
    }

    [Fact]
    public void CreateSlice_DeserializationMismatch_FallsBackToDefault()
    {
        // Persist an int envelope under a key, then try to restore as string
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 42,
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        // Restoring as string when an int was persisted — type mismatch
        var slice = manager.CreateSlice<string>("counter", defaultValue: "fallback");

        // Should fall back to default rather than crash
        slice.Value.ShouldBe("fallback");
        slice.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void CreateSlice_NullKey_ThrowsArgumentException()
    {
        using var manager = CreateManager();

        Should.Throw<ArgumentException>(() => manager.CreateSlice<int>(null!));
    }

    [Fact]
    public void CreateSlice_EmptyKey_ThrowsArgumentException()
    {
        using var manager = CreateManager();

        Should.Throw<ArgumentException>(() => manager.CreateSlice<int>(""));
    }

    [Fact]
    public void CreateSlice_AfterDispose_ThrowsObjectDisposedException()
    {
        var manager = CreateManager();
        manager.Dispose();

        Should.Throw<ObjectDisposedException>(() => manager.CreateSlice<int>("counter"));
    }

    [Fact]
    public void CreateSlice_RestoredSlice_IsStaleReflectsPersistedAt()
    {
        // Persisted 4 minutes ago, TTL is 5 minutes
        FakeState.Persist("data", new StateManager.PersistedEnvelope<string>
        {
            Value = "old",
            PersistedAt = DateTimeOffset.UtcNow.AddMinutes(-4)
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<string>("data", defaultValue: "new",
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(5));

        // Value should be restored (4 min < 5 min TTL)
        slice.Value.ShouldBe("old");
        slice.WasRestored.ShouldBeTrue();

        // But IsStale should reflect the ORIGINAL data age (4 min into a 5 min TTL),
        // not reset to fresh. After 1 more minute, this should go stale.
        // For now, 4 min < 5 min, so not yet stale.
        slice.IsStale.ShouldBeFalse();

        // The key assertion: LastUpdated should be close to PersistedAt, not UtcNow
        var age = DateTimeOffset.UtcNow - slice.LastUpdated;
        age.TotalMinutes.ShouldBeGreaterThan(3.5); // Should be ~4 minutes old
    }

    // -- Server cache (reload persistence) --------------------------------------

    [Fact]
    public void CreateSlice_ValueChange_UpdatesServerCache()
    {
        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("cached", defaultValue: 0);
        slice.Value = 77;

        // A second manager (simulating page reload) should restore from cache
        using var manager2 = CreateManager();
        var slice2 = manager2.CreateSlice<int>("cached", defaultValue: 0);

        slice2.Value.ShouldBe(77);
        slice2.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void CreateSlice_NoPrerenderButCached_RestoresFromCache()
    {
        // Pre-populate the cache directly
        Cache.Set("mykey", new StateManager.PersistedEnvelope<string>
        {
            Value = "from-cache",
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<string>("mykey", defaultValue: "default");

        slice.Value.ShouldBe("from-cache");
        slice.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void CreateSlice_CachedValueExpired_FallsBackToDefault()
    {
        Cache.Set("stale", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        });

        using var manager = CreateManager();
        var slice = manager.CreateSlice<int>("stale", defaultValue: 0,
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(30));

        slice.Value.ShouldBe(0);
        slice.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void CreateSlice_DuplicateKey_ThrowsInvalidOperationException()
    {
        using var manager = CreateManager();
        manager.CreateSlice<int>("counter");

        Should.Throw<InvalidOperationException>(() => manager.CreateSlice<int>("counter"));
    }

    // -- Dispose --------------------------------------------------------------

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var manager = CreateManager();
        manager.Dispose();
        manager.Dispose(); // should not throw
    }

    [Fact]
    public void Dispose_ClearsPersistCallbacks()
    {
        var manager = CreateManager();
        var slice = manager.CreateSlice<int>("counter");
        slice.Value = 42;
        manager.Dispose();

        // After dispose, persisting should not write anything
        FakeState.TriggerOnPersisting();

        FakeState.TryTake<StateManager.PersistedEnvelope<int>>("counter", out _).ShouldBeFalse();
    }
}
