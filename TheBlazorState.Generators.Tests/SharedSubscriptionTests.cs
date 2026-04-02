using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class SharedSubscriptionTests
{
    [Fact]
    public void Component_With_Injected_SharedState_Gets_Subscription()
    {
        var source = @"
using TheBlazorState.Attributes;
using TheBlazorState.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class CartState : INotifyStateChanged
{
    public event System.Action? StateChanged;
    public decimal Total { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public CartState Cart { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldContain("Cart.StateChanged");
        generatedSource.ShouldContain("__OnSharedStateChanged");
    }

    [Fact]
    public void Dispose_Unsubscribes_From_SharedState()
    {
        var source = @"
using TheBlazorState.Attributes;
using TheBlazorState.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class CartState : INotifyStateChanged
{
    public event System.Action? StateChanged;
    public decimal Total { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public CartState Cart { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldContain("Cart.StateChanged -=");
    }

    [Fact]
    public void Component_Without_SharedState_Has_No_Subscription()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public partial int Counter { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotContain("StateChanged");
        generatedSource.ShouldNotContain("__OnSharedStateChanged");
    }
}
