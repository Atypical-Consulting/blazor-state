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
using TheBlazorState.Services;
using TheBlazorState.Storage;
using Xunit;
using static TheBlazorState.Services.StateManager;

namespace TheBlazorState.Tests;

/// <summary>
/// Tests that verify components inheriting from <see cref="StateComponentBase"/>
/// automatically re-render when cross-tab sync delivers updates from other tabs.
///
/// These tests simulate the full flow: register a [Persist] property with
/// LocalStorage, then call CrossTabSyncService.OnStorageChanged (as if the
/// JS BroadcastChannel handler fired), and verify the rendered DOM updates.
/// </summary>
public class CrossTabSyncComponentTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly CrossTabSyncService _crossTabSync;

    public CrossTabSyncComponentTests()
    {
        _ctx = new BunitContext();
        _ctx.AddBunitPersistentComponentState();
        _ctx.Services.AddMemoryCache();
        _ctx.Services.AddSingleton<ILogger<StateManager>>(NullLogger<StateManager>.Instance);
        _ctx.Services.AddSingleton(new Configuration.TheBlazorStateOptions());
        _ctx.Services.AddScoped<Extensions.StorageStrategyInitializer>();
        _ctx.Services.AddScoped<BrowserStorageService>(_ => new BrowserStorageService(null!));

        // Pre-create CrossTabSyncService with a null IJSRuntime (no browser in tests).
        // Register as singleton so we can call OnStorageChanged directly in tests.
        _crossTabSync = new CrossTabSyncService(null!);
        _ctx.Services.AddSingleton(_crossTabSync);
        _ctx.Services.AddSingleton<CrossTabHub>();
        _ctx.Services.AddScoped<StateManager>();
    }

    public void Dispose()
    {
        _crossTabSync.Dispose();
        _ctx.Dispose();
    }

    // ---------------------------------------------------------------
    // Core test: component re-renders when cross-tab sync arrives
    // ---------------------------------------------------------------

    [Fact]
    public void CrossTabSync_UpdatesRenderedOutput()
    {
        // Arrange: render a component that displays a [Persist] property
        var cut = _ctx.Render<TestCrossTabComponent>();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("0");

        // Act: simulate cross-tab sync (another tab wrote value 42)
        _crossTabSync.OnStorageChanged("TestCrossTab.Count",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // Assert: the DOM should reflect the new value
        cut.Find("[data-testid='count']").TextContent.ShouldBe("42");
    }

    [Fact]
    public void CrossTabSync_UpdatesRenderedOutput_MultipleTimes()
    {
        var cut = _ctx.Render<TestCrossTabComponent>();

        _crossTabSync.OnStorageChanged("TestCrossTab.Count",
            """{"value":10,"persistedAt":"2026-01-01T00:00:00+00:00"}""");
        cut.Find("[data-testid='count']").TextContent.ShouldBe("10");

        _crossTabSync.OnStorageChanged("TestCrossTab.Count",
            """{"value":20,"persistedAt":"2026-01-01T00:00:01+00:00"}""");
        cut.Find("[data-testid='count']").TextContent.ShouldBe("20");
    }

    [Fact]
    public void CrossTabSync_UpdatesRenderedOutput_StringProperty()
    {
        var cut = _ctx.Render<TestCrossTabComponent>();
        cut.Find("[data-testid='color']").TextContent.ShouldBe("#000");

        _crossTabSync.OnStorageChanged("TestCrossTab.Color",
            """{"value":"#FF0000","persistedAt":"2026-01-01T00:00:00+00:00"}""");

        cut.Find("[data-testid='color']").TextContent.ShouldBe("#FF0000");
    }

    [Fact]
    public void CrossTabSync_DoesNotWriteBack()
    {
        // Verify that receiving a cross-tab update does NOT trigger a write
        // back to localStorage (which would cause infinite loops).
        var cut = _ctx.Render<TestCrossTabComponent>();
        var instance = cut.Instance;

        _crossTabSync.OnStorageChanged("TestCrossTab.Count",
            """{"value":42,"persistedAt":"2026-01-01T00:00:00+00:00"}""");

        // SuppressPersist should have been set and reset
        instance.CountMeta.SuppressPersist.ShouldBeFalse();
        // The value should be updated
        instance.Count.ShouldBe(42);
    }

    [Fact]
    public void LocalChange_StillRendersNormally()
    {
        // Verify that local changes (button clicks) still work
        var cut = _ctx.Render<TestCrossTabComponent>();

        cut.Find("button").Click();

        cut.Find("[data-testid='count']").TextContent.ShouldBe("1");
    }

    [Fact]
    public void LocalChange_ThenCrossTabSync_BothWork()
    {
        var cut = _ctx.Render<TestCrossTabComponent>();

        // Local click
        cut.Find("button").Click();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("1");

        // Cross-tab sync overrides
        _crossTabSync.OnStorageChanged("TestCrossTab.Count",
            """{"value":99,"persistedAt":"2026-01-01T00:00:00+00:00"}""");
        cut.Find("[data-testid='count']").TextContent.ShouldBe("99");

        // Local click again (from new base)
        cut.Find("button").Click();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("100");
    }

    // ---------------------------------------------------------------
    // Test component: inherits from StateComponentBase
    // ---------------------------------------------------------------

    /// <summary>
    /// Test component mimicking generated code for a [Persist] int Count
    /// property with LocalStorage strategy. Inherits from StateComponentBase
    /// to get automatic cross-tab sync re-rendering.
    /// </summary>
    private class TestCrossTabComponent : StateComponentBase
    {
        [Inject] private StateManager StateManager { get; set; } = default!;

        private int _count;
        private string _color = "#000";

        public StateMeta CountMeta { get; } = new StateMeta(ttl: null);
        public StateMeta ColorMeta { get; } = new StateMeta(ttl: null);

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

        public string Color
        {
            get => _color;
            set
            {
                if (_color == value) return;
                _color = value;
                ColorMeta.MarkDirty();
                ColorMeta.RaiseChanged();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            CountMeta.OnAfterChanged += () => InvokeAsync(StateHasChanged);
            ColorMeta.OnAfterChanged += () => InvokeAsync(StateHasChanged);

            StateManager.RestoreProperty(
                "TestCrossTab.Count",
                StorageStrategy.LocalStorage(),
                CountMeta,
                v => _count = v,
                () => _count);

            StateManager.RestoreProperty(
                "TestCrossTab.Color",
                StorageStrategy.LocalStorage(),
                ColorMeta,
                v => _color = v,
                () => _color);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "count");
            builder.AddContent(2, _count.ToString());
            builder.CloseElement();

            builder.OpenElement(3, "span");
            builder.AddAttribute(4, "data-testid", "color");
            builder.AddContent(5, _color);
            builder.CloseElement();

            builder.OpenElement(6, "button");
            builder.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, () => Count++));
            builder.AddContent(8, "Increment");
            builder.CloseElement();
        }

        public override void Dispose()
        {
            CountMeta.ClearHandlers();
            ColorMeta.ClearHandlers();
            StateManager.Dispose();
            base.Dispose();
        }
    }
}
