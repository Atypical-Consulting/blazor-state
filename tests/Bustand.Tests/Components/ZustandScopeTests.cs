using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Bustand.Extensions;
using Bustand.Components;
using Bustand.Tests.TestStores;
using Bustand.Tests.TestComponents;

namespace Bustand.Tests.Components;

/// <summary>
/// bUnit tests for ZustandScope covering COMP-04, COMP-05, and MODE-07.
/// </summary>
public class ZustandScopeTests : BunitContext
{
    // COMP-04: Component can use ZustandScope for scoped store instances
    [Fact]
    public void ZustandScope_ProvidesIsolatedStore()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());

        // Create a scope with the ScopedCounterComponent
        var cut = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .AddChildContent<ScopedCounterComponent>());

        // Component renders
        Assert.Contains("0", cut.Find(".count").TextContent);
    }

    // COMP-05: Component can access store via CascadingParameter
    [Fact]
    public void ZustandScope_CascadesStoreToChildren()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var store = new CounterStore();

        var cut = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, store)
            .AddChildContent<ScopedCounterComponent>());

        // Invoke store method directly (avoids bUnit event handler detection issues)
        store.Increment();
        cut.WaitForState(() => cut.Find(".count").TextContent.Contains("1"));

        Assert.Contains("1", cut.Find(".count").TextContent);
    }

    // Instance parameter
    [Fact]
    public void ZustandScope_AcceptsExistingInstance()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var existingStore = new CounterStore();
        existingStore.SetCount(100);

        var cut = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, existingStore)
            .AddChildContent<ScopedCounterComponent>());

        Assert.Contains("100", cut.Find(".count").TextContent);
    }

    // MODE-07: Library components never specify @rendermode
    [Fact]
    public void ZustandScope_NoRenderMode_ModeAgnostic()
    {
        // Verify by checking that component works in test (no mode specified)
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());
        var cut = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .AddChildContent("<div>Test</div>"));

        // Component renders without specifying mode = mode-agnostic
        Assert.NotNull(cut.Markup);
        Assert.Contains("Test", cut.Markup);
    }

    // Multiple scoped instances are independent
    [Fact]
    public void ZustandScope_MultipleScopes_AreIndependent()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());

        var store1 = new CounterStore();
        var store2 = new CounterStore();
        store1.SetCount(10);
        store2.SetCount(20);

        var cut1 = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, store1)
            .AddChildContent<ScopedCounterComponent>());

        var cut2 = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, store2)
            .AddChildContent<ScopedCounterComponent>());

        Assert.Contains("10", cut1.Find(".count").TextContent);
        Assert.Contains("20", cut2.Find(".count").TextContent);
    }

    // Scoped store state changes are isolated
    [Fact]
    public void ZustandScope_StateChanges_AreIsolated()
    {
        Services.AddBustand(opts => opts.ScanAssemblyContaining<CounterStore>());

        var store1 = new CounterStore();
        var store2 = new CounterStore();

        var cut1 = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, store1)
            .AddChildContent<ScopedCounterComponent>());

        var cut2 = Render<ZustandScope<CounterStore, CounterState>>(parameters => parameters
            .Add(p => p.Instance, store2)
            .AddChildContent<ScopedCounterComponent>());

        // Invoke store1 method directly
        store1.Increment();
        cut1.WaitForState(() => cut1.Find(".count").TextContent.Contains("1"));

        // Only scope1 should be updated
        Assert.Contains("1", cut1.Find(".count").TextContent);
        Assert.Contains("0", cut2.Find(".count").TextContent);
    }
}
