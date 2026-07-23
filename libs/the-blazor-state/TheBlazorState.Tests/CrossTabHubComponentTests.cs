using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TheBlazorState.Abstractions;
using TheBlazorState.Components;
using TheBlazorState.Configuration;
using TheBlazorState.Extensions;
using TheBlazorState.Services;
using TheBlazorState.Storage;
using Xunit;
using static TheBlazorState.Services.StateManager;

namespace TheBlazorState.Tests;

/// <summary>
/// End-to-end test: TWO circuits share a singleton CrossTabHub.
/// When circuit A changes a value, circuit B's component re-renders.
/// This is the definitive test for cross-tab sync in Blazor Server.
///
/// Because CrossTabHub dispatches callbacks asynchronously via Task.Run,
/// tests use bUnit's WaitForState to wait for the re-render.
/// </summary>
public class CrossTabHubComponentTests : IDisposable
{
    private readonly CrossTabHub _hub = new();
    private readonly BunitContext _ctxB;

    // Circuit A: standalone StateManager (simulates another tab's server-side circuit)
    private readonly StateManager _managerA;
    private readonly StateMeta _metaA = new(ttl: null);
    private int _valueA;

    public CrossTabHubComponentTests()
    {
        // --- Circuit A: register property via standalone StateManager ---
        var cacheA = new MemoryCache(new MemoryCacheOptions());
        var ctxA = new BunitContext();
        ctxA.AddBunitPersistentComponentState();

        _managerA = new StateManager(
            ctxA.Services.GetRequiredService<PersistentComponentState>(),
            cacheA,
            NullLogger<StateManager>.Instance,
            new TheBlazorStateOptions(),
            new StorageStrategyInitializer(new BrowserStorageService(null!), cacheA),
            new CrossTabSyncService(null!),
            _hub);

        _managerA.RestoreProperty(
            "Test.Counter",
            StorageStrategy.LocalStorage(),
            _metaA,
            v => _valueA = v,
            () => _valueA);

        // --- Circuit B: bUnit context with rendered component ---
        _ctxB = new BunitContext();
        _ctxB.AddBunitPersistentComponentState();
        _ctxB.Services.AddMemoryCache();
        _ctxB.Services.AddSingleton<ILogger<StateManager>>(NullLogger<StateManager>.Instance);
        _ctxB.Services.AddSingleton(new TheBlazorStateOptions());
        _ctxB.Services.AddScoped<StorageStrategyInitializer>();
        _ctxB.Services.AddScoped<BrowserStorageService>(_ => new BrowserStorageService(null!));
        _ctxB.Services.AddSingleton(_hub); // shared hub!
        _ctxB.Services.AddSingleton(new CrossTabSyncService(null!));
        _ctxB.Services.AddScoped<StateManager>();
    }

    public void Dispose()
    {
        _managerA.Dispose();
        _hub.Dispose();
        _ctxB.Dispose();
    }

    private void CircuitA_SetValue(int value)
    {
        _valueA = value;
        _metaA.MarkDirty();
        _metaA.RaiseChanged(); // triggers eager handler → publishes to hub (async)
    }

    [Fact]
    public void CircuitA_Change_UpdatesCircuitB_Component()
    {
        var cut = _ctxB.Render<HubTestComponent>();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("0");

        CircuitA_SetValue(42);

        // Wait for async hub dispatch + component re-render
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "42");
    }

    [Fact]
    public void CircuitA_MultipleChanges_AllReflectedInCircuitB()
    {
        var cut = _ctxB.Render<HubTestComponent>();

        CircuitA_SetValue(10);
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "10");

        CircuitA_SetValue(20);
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "20");

        CircuitA_SetValue(30);
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "30");
    }

    [Fact]
    public void CircuitB_LocalClick_StillWorks()
    {
        var cut = _ctxB.Render<HubTestComponent>();
        cut.Find("button").Click();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("1");
    }

    [Fact]
    public void CircuitA_Change_ThenCircuitB_LocalClick_Correct()
    {
        var cut = _ctxB.Render<HubTestComponent>();

        CircuitA_SetValue(42);
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "42");

        cut.Find("button").Click();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("43");
    }

    // ---------------------------------------------------------------
    // Echo-back prevention: publisher should not get CrossTab entries
    // ---------------------------------------------------------------

    [Fact]
    public async Task CircuitA_LocalChange_ShouldNotReceiveCrossTabEchoFromHub()
    {
        // Reproduces the cross-tab echo-back bug:
        // When Circuit A changes a value, the hub notifies Circuit B.
        // Circuit A's ChangeLog should contain ONLY a Local entry.
        var cut = _ctxB.Render<HubTestComponent>();

        CircuitA_SetValue(42);

        // Wait for hub to dispatch to Circuit B
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "42");

        // Assert: Circuit A's ChangeLog has only Local entries
        var crossTabEntries = _metaA.ChangeLog
            .Where(e => e.Source == Abstractions.ChangeSource.CrossTab)
            .ToList();
        crossTabEntries.ShouldBeEmpty(
            "Publisher should not receive its own change back as CrossTab");
    }

    [Fact]
    public async Task CircuitA_LocalChange_HubEchoBackWithSameValue_ShouldBeIgnored()
    {
        // Simulates a stale prerender circuit: a THIRD subscription on the hub
        // receives Circuit A's publish and echoes it back via hub (different circuitId).
        // Circuit A should ignore it because the value is already current.
        var cut = _ctxB.Render<HubTestComponent>();

        // Create a "stale" subscription that echoes back to Circuit A
        string? echoJson = null;
        var staleSub = _hub.Subscribe("Test.Counter", (_, json) =>
        {
            echoJson = json;
        }, subscriberId: "stale-prerender-circuit");

        CircuitA_SetValue(42);

        // Wait for hub to dispatch to all subscribers
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "42");
        await Task.Delay(50); // ensure stale callback fires

        // The stale subscription received the notification
        echoJson.ShouldNotBeNull();

        // Now simulate the echo-back: stale circuit publishes with different circuitId
        // This would reach Circuit A because "stale-prerender-circuit" != circuitA
        _hub.Publish("Test.Counter", echoJson!, publisherId: "stale-prerender-circuit");
        await Task.Delay(100); // wait for async dispatch

        // Assert: Circuit A should NOT have a CrossTab entry
        var crossTabEntries = _metaA.ChangeLog
            .Where(e => e.Source == Abstractions.ChangeSource.CrossTab)
            .ToList();
        crossTabEntries.ShouldBeEmpty(
            "Echo-back from stale circuit should be suppressed (value already current)");
    }

    [Fact]
    public async Task CircuitB_ShouldGetExactlyOneCrossTabEntry()
    {
        // Verifies that the receiving circuit doesn't get duplicate entries
        // from both hub and JS sync paths arriving at different times.
        var cut = _ctxB.Render<HubTestComponent>();
        var component = cut.Instance;

        CircuitA_SetValue(42);

        // Wait for Circuit B to receive the update
        cut.WaitForState(() =>
            cut.Find("[data-testid='count']").TextContent == "42");
        await Task.Delay(50); // settle

        // Assert: exactly one CrossTab entry, not duplicates
        var crossTabEntries = component.CountMeta.ChangeLog
            .Where(e => e.Source == Abstractions.ChangeSource.CrossTab)
            .ToList();
        crossTabEntries.Count.ShouldBe(1,
            "Receiver should get exactly one CrossTab entry per change");
    }

    /// <summary>
    /// Test component for Circuit B. Inherits StateComponentBase.
    /// </summary>
    private class HubTestComponent : StateComponentBase
    {
        [Inject] private StateManager StateManager { get; set; } = default!;

        private int _count;
        public StateMeta CountMeta { get; } = new StateMeta(ttl: null);

        public int Count
        {
            get => _count;
            set
            {
                if (_count == value) return;
                _count = value;
                CountMeta.MarkDirty();
                CountMeta.RaiseChanged();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            CountMeta.OnAfterChanged += () => InvokeAsync(StateHasChanged);

            StateManager.RestoreProperty(
                "Test.Counter",
                StorageStrategy.LocalStorage(),
                CountMeta,
                v => _count = v,
                () => _count);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "count");
            builder.AddContent(2, _count.ToString());
            builder.CloseElement();

            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => Count++));
            builder.AddContent(5, "Increment");
            builder.CloseElement();
        }

        public override void Dispose()
        {
            CountMeta.ClearHandlers();
            StateManager.Dispose();
            base.Dispose();
        }
    }
}
