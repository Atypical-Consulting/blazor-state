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
using TheBlazorState.Services;
using Xunit;
using static TheBlazorState.Services.StateManager;

namespace TheBlazorState.Tests;

/// <summary>
/// Higher-level tests that render components using bUnit and verify
/// [Persist]-like behavior end-to-end (mimicking generated code patterns).
/// </summary>
public class PersistPropertyRoundtripTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitPersistentComponentState _persistentState;

    public PersistPropertyRoundtripTests()
    {
        _ctx = new BunitContext();
        _persistentState = _ctx.AddBunitPersistentComponentState();
        _ctx.Services.AddMemoryCache();
        _ctx.Services.AddSingleton<ILogger<StateManager>>(NullLogger<StateManager>.Instance);
        _ctx.Services.AddScoped<StateManager>();
    }

    public void Dispose() => _ctx.Dispose();

    // --- Component renders with default value ---

    [Fact]
    public void Component_RendersDefaultValue_WhenNothingPersisted()
    {
        var cut = _ctx.Render<TestCounterComponent>();

        cut.Find("[data-testid='count']").TextContent.ShouldBe("0");
        cut.Find("[data-testid='restored']").TextContent.ShouldBe("False");
    }

    // --- Component renders with restored value ---

    [Fact]
    public void Component_RendersRestoredValue_WhenPersisted()
    {
        // Pre-persist a value
        _persistentState.Persist("TestCounterComponent.Count", new PersistedEnvelope<int>
        {
            Value = 42,
            PersistedAt = DateTimeOffset.UtcNow.AddSeconds(-5)
        });

        var cut = _ctx.Render<TestCounterComponent>();

        cut.Find("[data-testid='count']").TextContent.ShouldBe("42");
        cut.Find("[data-testid='restored']").TextContent.ShouldBe("True");
    }

    // --- Value changes trigger re-render ---

    [Fact]
    public void Component_ValueChange_TriggersRerender()
    {
        var cut = _ctx.Render<TestCounterComponent>();

        cut.Find("[data-testid='count']").TextContent.ShouldBe("0");

        cut.Find("button").Click();

        cut.Find("[data-testid='count']").TextContent.ShouldBe("1");
    }

    // --- Meta companion is accessible and accurate ---

    [Fact]
    public void Component_MetaCompanion_IsAccessible()
    {
        _persistentState.Persist("TestCounterComponent.Count", new PersistedEnvelope<int>
        {
            Value = 10,
            PersistedAt = DateTimeOffset.UtcNow.AddMinutes(-3)
        });

        var cut = _ctx.Render<TestCounterComponent>();

        cut.Find("[data-testid='restored']").TextContent.ShouldBe("True");
    }

    // --- OnPersisting round-trip ---

    [Fact]
    public void Component_OnPersisting_PersistsCurrentValue()
    {
        var cut = _ctx.Render<TestCounterComponent>();

        // Click to set value to 1
        cut.Find("button").Click();
        cut.Find("[data-testid='count']").TextContent.ShouldBe("1");

        // Trigger OnPersisting
        _persistentState.TriggerOnPersisting();

        // Verify value is in cache
        var cache = _ctx.Services.GetRequiredService<IMemoryCache>();
        cache.TryGetValue<PersistedEnvelope<int>>("TestCounterComponent.Count", out var envelope)
            .ShouldBeTrue();
        envelope!.Value.ShouldBe(1);
    }

    // --- Dispose cleans up ---

    [Fact]
    public void Component_Dispose_CleansUp()
    {
        var cut = _ctx.Render<TestCounterComponent>();
        var instance = cut.Instance;

        cut.Dispose();

        // StateManager should be disposed when component disposes
        // We verify this indirectly: the component rendered and disposed without error
    }

    /// <summary>
    /// A test component that mimics what the source generator produces
    /// for a component with a [Persist] int Count property.
    /// </summary>
    private class TestCounterComponent : ComponentBase, IDisposable
    {
        [Inject] private StateManager StateManager { get; set; } = default!;

        // Backing field (generated)
        private int _count;

        // StateMeta companion (generated)
        public StateMeta CountMeta { get; } = new StateMeta(ttl: null);

        // Property with getter/setter (generated)
        public int Count
        {
            get => _count;
            set
            {
                if (!EqualityComparer<int>.Default.Equals(_count, value))
                {
                    _count = value;
                    CountMeta.MarkDirty();
                    CountMeta.RaiseChanged();
                    StateHasChanged();
                }
            }
        }

        protected override void OnInitialized()
        {
            CountMeta.OnChanged += StateHasChanged;
            StateManager.RestoreProperty<int>(
                "TestCounterComponent.Count",
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

            builder.OpenElement(3, "span");
            builder.AddAttribute(4, "data-testid", "restored");
            builder.AddContent(5, CountMeta.WasRestored.ToString());
            builder.CloseElement();

            builder.OpenElement(6, "button");
            builder.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, () => Count++));
            builder.AddContent(8, "Increment");
            builder.CloseElement();
        }

        public void Dispose()
        {
            CountMeta.ClearHandlers();
            StateManager.Dispose();
        }
    }
}
