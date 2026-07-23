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

namespace TheBlazorState.Tests;

/// <summary>
/// Tests that verify the cross-tab sync mechanism works correctly
/// at the StateManager + CrossTabSyncService level.
///
/// These tests simulate what happens when another browser tab writes to
/// localStorage and the JS layer calls CrossTabSyncService.OnStorageChanged.
/// No browser or JS is involved — we call OnStorageChanged directly.
/// </summary>
public class CrossTabSyncTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly IMemoryCache _cache;
    private readonly CrossTabSyncService _crossTabSync;

    public CrossTabSyncTests()
    {
        _ctx = new BunitContext();
        _ctx.AddBunitPersistentComponentState();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _ctx.Services.AddSingleton(_cache);
        _ctx.Services.AddSingleton(NullLogger<StateManager>.Instance);
        _crossTabSync = new CrossTabSyncService(
            _ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());
    }

    public void Dispose()
    {
        _crossTabSync.Dispose();
        _cache.Dispose();
        _ctx.Dispose();
    }

    private StateManager CreateManager()
    {
        var options = new TheBlazorStateOptions();
        var browserStorage = new BrowserStorageService(null!);
        var initializer = new StorageStrategyInitializer(browserStorage, _cache);
        return new StateManager(
            _ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.PersistentComponentState>(),
            _cache,
            NullLogger<StateManager>.Instance,
            options,
            initializer,
            _crossTabSync,
            new CrossTabHub());
    }

    // ---------------------------------------------------------------
    // Core cross-tab sync: OnStorageChanged updates backing field
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_UpdatesBackingField()
    {
        // Arrange: register a [Persist] property with LocalStorage
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: simulate cross-tab sync (another tab wrote value 42)
        var json = """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""";
        _crossTabSync.OnStorageChanged("Test.Counter", json);

        // Assert: backing field updated
        value.ShouldBe(42);
    }

    [Fact]
    public void OnStorageChanged_MarksDirty()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        meta.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void OnStorageChanged_FiresOnChanged()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        bool onChangedFired = false;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        meta.OnChanged += () => onChangedFired = true;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        onChangedFired.ShouldBeTrue();
    }

    [Fact]
    public void OnStorageChanged_FiresOnAfterChanged()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        bool afterChangedFired = false;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        meta.OnAfterChanged += () => afterChangedFired = true;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        afterChangedFired.ShouldBeTrue();
    }

    // ---------------------------------------------------------------
    // OnAfterChanged fires AFTER OnChanged (render ordering fix)
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_OnAfterChanged_FiresAfterOnChanged()
    {
        // This is the critical test: OnAfterChanged must fire AFTER
        // all OnChanged handlers complete, so that any state updates
        // made by OnChanged handlers are visible when the render runs.
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        int valueSeenByOnChanged = -1;
        int valueSeenByOnAfterChanged = -1;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        meta.OnChanged += () => valueSeenByOnChanged = value;
        meta.OnAfterChanged += () => valueSeenByOnAfterChanged = value;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: both see the updated value
        valueSeenByOnChanged.ShouldBe(42);
        valueSeenByOnAfterChanged.ShouldBe(42);
    }

    [Fact]
    public void OnStorageChanged_OnAfterChanged_SeesStateFromOnChangedHandlers()
    {
        // Simulates the demo pattern: OnChanged handler updates a
        // secondary value. OnAfterChanged (used for rendering) must
        // see that secondary value already updated.
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int savedCounter = 0;
        int sharedCounter = 0; // simulates CrossState.SharedCounter

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => savedCounter = v,
            () => savedCounter);

        // Simulate the reverse bridge: OnChanged propagates to shared state
        meta.OnChanged += () => sharedCounter = savedCounter;

        int sharedCounterSeenByRender = -1;
        meta.OnAfterChanged += () => sharedCounterSeenByRender = sharedCounter;

        // Act: cross-tab sync delivers value 42
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: the render callback sees the updated shared counter
        sharedCounterSeenByRender.ShouldBe(42);
    }

    // ---------------------------------------------------------------
    // SuppressPersist prevents write-back loop
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_SuppressesPersistDuringCallback()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        bool suppressWasTrueDuringOnChanged = false;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // The eager handler (subscribed first) checks SuppressPersist.
        // We add our own handler after to verify it was true.
        meta.OnChanged += () => suppressWasTrueDuringOnChanged = meta.SuppressPersist;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        suppressWasTrueDuringOnChanged.ShouldBeTrue();
    }

    [Fact]
    public void OnStorageChanged_ResetsSuppressPersistAfterRaiseChanged()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: SuppressPersist is reset after RaiseChanged completes
        meta.SuppressPersist.ShouldBeFalse();
    }

    // ---------------------------------------------------------------
    // Server cache is updated (even when SuppressPersist is true)
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_UpdatesServerCache()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: server cache was updated by the eager handler
        _cache.TryGetValue<StateManager.PersistedEnvelope<int>>("Test.Counter", out var cached)
            .ShouldBeTrue();
        cached!.Value.ShouldBe(42);
    }

    // ---------------------------------------------------------------
    // Case-insensitive JSON deserialization (Blazor uses camelCase)
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_HandlesCamelCaseJson()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: camelCase JSON (as produced by Blazor's SignalR serialization)
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":99,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        value.ShouldBe(99);
    }

    [Fact]
    public void OnStorageChanged_HandlesPascalCaseJson()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: PascalCase JSON
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"Value":77,"PersistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        value.ShouldBe(77);
    }

    // ---------------------------------------------------------------
    // AfterCrossTabChange event fires
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_FiresAfterCrossTabChangeEvent()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        bool eventFired = false;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        _crossTabSync.AfterCrossTabChange += () => eventFired = true;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        eventFired.ShouldBeTrue();
    }

    [Fact]
    public void OnStorageChanged_AfterCrossTabChange_FiresAfterCallback()
    {
        // AfterCrossTabChange should fire AFTER the callback completes,
        // so components subscribing to it see the fully updated state.
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;
        int valueSeenByEvent = -1;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        _crossTabSync.AfterCrossTabChange += () => valueSeenByEvent = value;

        // Act
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        valueSeenByEvent.ShouldBe(42);
    }

    // ---------------------------------------------------------------
    // Unregistered keys are ignored
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_UnregisteredKey_DoesNothing()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: different key
        _crossTabSync.OnStorageChanged("Other.Key",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: value unchanged
        value.ShouldBe(0);
    }

    // ---------------------------------------------------------------
    // Malformed JSON is handled gracefully
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_MalformedJson_DoesNotThrow()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: malformed JSON
        Should.NotThrow(() =>
            _crossTabSync.OnStorageChanged("Test.Counter", "not valid json"));

        // Assert: value unchanged
        value.ShouldBe(0);
    }

    // ---------------------------------------------------------------
    // Only LocalStorage strategy registers cross-tab sync
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_SessionStorage_NotRegistered()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        // Register with SessionStorage (not LocalStorage)
        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.SessionStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: simulate cross-tab sync
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: value unchanged (SessionStorage doesn't register for cross-tab)
        value.ShouldBe(0);
    }

    // ---------------------------------------------------------------
    // String properties work
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_StringProperty_Works()
    {
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        string value = "initial";

        manager.RestoreProperty(
            "Test.Color",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act
        _crossTabSync.OnStorageChanged("Test.Color",
            """{"value":"#FF0000","persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert
        value.ShouldBe("#FF0000");
    }

    // ---------------------------------------------------------------
    // Echo-back prevention: JS sync with same value should be ignored
    // ---------------------------------------------------------------

    [Fact]
    public void OnStorageChanged_SameValueAsCurrentLocal_ShouldNotCreateCrossTabEntry()
    {
        // Reproduces: Tab A changes value locally, then receives its own
        // change back via JS storage event (browser quirk or stale subscription).
        // The echo-back should be suppressed — no CrossTab log entry.
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Simulate local change (as the generated property setter would)
        value = 42;
        meta.MarkDirty();
        meta.RaiseChanged(); // logs ● (Local) entry

        // Act: simulate echo-back via JS storage event with same value
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: ChangeLog should have only the Local entry, no CrossTab echo
        meta.ChangeLog.Count.ShouldBe(1);
        meta.ChangeLog[0].Source.ShouldBe(ChangeSource.Local);
    }

    [Fact]
    public void OnStorageChanged_DifferentValue_ShouldCreateCrossTabEntry()
    {
        // Verify that legitimate cross-tab updates still work
        var manager = CreateManager();
        var meta = new StateMeta(ttl: null);
        int value = 0;

        manager.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            meta,
            v => value = v,
            () => value);

        // Act: cross-tab sync delivers a genuinely new value
        _crossTabSync.OnStorageChanged("Test.Counter",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: value updated and CrossTab entry logged
        value.ShouldBe(42);
        meta.ChangeLog.Count.ShouldBe(1);
        meta.ChangeLog[0].Source.ShouldBe(ChangeSource.CrossTab);
    }
}
