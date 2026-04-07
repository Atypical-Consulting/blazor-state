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
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial decimal Total { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public CartState Cart { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        // PersistEmitter emits partial method hooks
        generatedSource.ShouldContain("__SubscribeToSharedState");
        generatedSource.ShouldContain("__OnSharedStateChanged");
        // InjectSubscriptionGenerator emits actual subscription in partial method body
        generatedSource.ShouldContain("Cart.StateChanged");
    }

    [Fact]
    public void Dispose_Unsubscribes_From_SharedState()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial decimal Total { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public CartState Cart { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        // PersistEmitter calls __UnsubscribeFromSharedState in Dispose
        generatedSource.ShouldContain("__UnsubscribeFromSharedState");
        // InjectSubscriptionGenerator emits the actual unsubscribe
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
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        // PersistEmitter still emits the partial method declarations (no-ops without InjectSubscriptionGenerator)
        generatedSource.ShouldContain("partial void __SubscribeToSharedState");
        generatedSource.ShouldContain("partial void __UnsubscribeFromSharedState");
        // But no actual StateChanged subscriptions
        generatedSource.ShouldNotContain("StateChanged +=");
        generatedSource.ShouldNotContain("StateChanged -=");
    }
}
