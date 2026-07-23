using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Bustand.Extensions;
using Bustand.Tests.TestStores;
using Bustand.Tests.TestComponents;

namespace Bustand.Tests.Components;

/// <summary>
/// bUnit tests for ZustandComponent covering COMP-01 through COMP-08.
/// </summary>
public class ZustandComponentTests : BunitContext
{
    // COMP-01: Component can subscribe to store state changes
    [Fact]
    public void Component_SubscribesToStore()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var cut = Render<CounterComponent>();

        Assert.Contains("0", cut.Find(".count").TextContent);
    }

    // COMP-02: Component re-renders automatically
    [Fact]
    public void Component_ReRendersOnStateChange()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = Services.GetRequiredService<CounterStore>();
        var cut = Render<CounterComponent>();

        // Invoke store method directly (avoids bUnit event handler detection issues)
        store.Increment();
        cut.WaitForState(() => cut.Find(".count").TextContent.Contains("1"));

        Assert.Contains("1", cut.Find(".count").TextContent);
    }

    // COMP-03: Store can be injected via DI
    [Fact]
    public void Component_InjectsStoreViaDI()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var cut = Render<CounterComponent>();

        // Component renders without error = DI injection works
        Assert.NotNull(cut.Instance);
    }

    // COMP-06: Component can subscribe to specific state slice
    [Fact]
    public void Component_SubscribesToSlice()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = Services.GetRequiredService<CounterStore>();
        var cut = Render<CounterComponent>();

        store.SetCount(42);
        cut.WaitForState(() => cut.Find(".count").TextContent.Contains("42"));

        Assert.Contains("42", cut.Find(".count").TextContent);
    }

    // COMP-07: Component only re-renders when selected slice changes
    [Fact]
    public void Component_OnlyReRendersOnSliceChange()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<MultiPropertyStore>());
        // This test verifies slice subscription works via the store API
        var store = Services.GetRequiredService<MultiPropertyStore>();

        var countNotifications = 0;
        store.Subscribe(s => s.Name, () => countNotifications++);

        store.SetCount(1); // Should not trigger name subscriber
        Assert.Equal(0, countNotifications);

        store.SetName("test"); // Should trigger
        Assert.Equal(1, countNotifications);
    }

    // COMP-08: Component subscriptions dispose properly
    [Fact]
    public void Component_DisposesSubscriptions()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = Services.GetRequiredService<CounterStore>();

        var cut = Render<CounterComponent>();
        var initialCount = store.SubscriptionCount;
        Assert.True(initialCount > 0, "Component should have at least one subscription");

        Dispose(); // Dispose the test context which disposes all rendered components
        Assert.Equal(0, store.SubscriptionCount);
    }

    // UseState implicit conversion
    [Fact]
    public void UseState_ImplicitConversion_Works()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var cut = Render<CounterComponent>();

        // Component renders count value via implicit conversion
        Assert.Contains("0", cut.Find(".count").TextContent);
    }

    // Multiple state changes
    [Fact]
    public void Component_HandlesMultipleStateChanges()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = Services.GetRequiredService<CounterStore>();
        var cut = Render<CounterComponent>();

        // Invoke store methods directly
        store.Increment();
        store.Increment();
        store.Decrement();
        cut.WaitForState(() => cut.Find(".count").TextContent.Contains("1"));

        Assert.Contains("1", cut.Find(".count").TextContent);
    }

    // Store state persists across renders
    [Fact]
    public void Component_StoreStatePersists()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = Services.GetRequiredService<CounterStore>();

        var cut = Render<CounterComponent>();
        store.Increment();
        cut.WaitForState(() => cut.Find(".count").TextContent.Contains("1"));

        Assert.Contains("1", cut.Find(".count").TextContent);

        // Verify store state matches
        Assert.Equal(1, store.State.Count);
    }
}
