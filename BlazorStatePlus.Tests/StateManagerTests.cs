using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Services;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace BlazorStatePlus.Tests;

public class StateManagerTests : BunitContext
{
    private BunitPersistentComponentState FakeState { get; set; } = null!;

    public StateManagerTests()
    {
        FakeState = AddBunitPersistentComponentState();
    }

    private StateManager CreateManager()
    {
        var pcs = Services.GetRequiredService<PersistentComponentState>();
        return new StateManager(pcs, NullLogger<StateManager>.Instance);
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

    // -- CreateAndInit --------------------------------------------------------

    [Fact]
    public void CreateAndInit_NoPersistedState_CallsFactory()
    {
        using var manager = CreateManager();
        bool factoryCalled = false;

        var slice = manager.CreateAndInit("counter", () =>
        {
            factoryCalled = true;
            return 42;
        });

        factoryCalled.ShouldBeTrue();
        slice.Value.ShouldBe(42);
    }

    [Fact]
    public void CreateAndInit_WithPersistedState_SkipsFactory()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        bool factoryCalled = false;

        var slice = manager.CreateAndInit("counter", () =>
        {
            factoryCalled = true;
            return 42;
        });

        factoryCalled.ShouldBeFalse();
        slice.Value.ShouldBe(99);
    }

    [Fact]
    public void CreateAndInit_WithStalePersistedState_CallsFactory()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        });

        using var manager = CreateManager();

        var slice = manager.CreateAndInit("counter", () => 42,
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(30));

        slice.Value.ShouldBe(42);
    }

    // -- CreateAndInitAsync ---------------------------------------------------

    [Fact]
    public async Task CreateAndInitAsync_NoPersistedState_CallsFactory()
    {
        using var manager = CreateManager();
        bool factoryCalled = false;

        var slice = await manager.CreateAndInitAsync("counter", async () =>
        {
            factoryCalled = true;
            return 42;
        });

        factoryCalled.ShouldBeTrue();
        slice.Value.ShouldBe(42);
    }

    [Fact]
    public async Task CreateAndInitAsync_WithPersistedState_SkipsFactory()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow
        });

        using var manager = CreateManager();
        bool factoryCalled = false;

        var slice = await manager.CreateAndInitAsync("counter", async () =>
        {
            factoryCalled = true;
            return 42;
        });

        factoryCalled.ShouldBeFalse();
        slice.Value.ShouldBe(99);
    }

    [Fact]
    public async Task CreateAndInitAsync_WithStalePersistedState_CallsFactory()
    {
        FakeState.Persist("counter", new StateManager.PersistedEnvelope<int>
        {
            Value = 99,
            PersistedAt = DateTimeOffset.UtcNow.AddHours(-2)
        });

        using var manager = CreateManager();

        var slice = await manager.CreateAndInitAsync("counter", async () => 42,
            configure: o => o.TimeToLive = TimeSpan.FromMinutes(30));

        slice.Value.ShouldBe(42);
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
