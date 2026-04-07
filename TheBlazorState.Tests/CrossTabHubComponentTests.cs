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
