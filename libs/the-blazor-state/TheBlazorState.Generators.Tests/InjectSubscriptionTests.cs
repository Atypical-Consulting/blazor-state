using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class InjectSubscriptionTests
{
    [Fact]
    public void Component_With_Injected_SharedState_Gets_Subscription()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class ThemeState
{
    [Shared]
    public partial string Theme { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public ThemeState Theme { get; set; } = default!;
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("Theme.StateChanged += __OnSharedStateChanged");
        generatedSource.ShouldContain("Theme.StateChanged -= __OnSharedStateChanged");
        generatedSource.ShouldContain("InvokeAsync(StateHasChanged)");
    }

    [Fact]
    public void Component_Without_SharedState_Gets_No_Subscription()
    {
        var source = @"
using Microsoft.AspNetCore.Components;

namespace Test;

public class RegularService { }

public partial class MyComponent : ComponentBase
{
    [Inject]
    public RegularService Svc { get; set; } = default!;
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        // Should not contain subscription code (only generated code from other generators if any)
        if (generatedSource != null)
        {
            generatedSource.ShouldNotContain("StateChanged");
        }
    }

    [Fact]
    public void Component_With_Persist_Gets_Partial_Methods()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class ProjectState
{
    [Shared]
    public partial int SelectedId { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public ProjectState Project { get; set; } = default!;

    [Persist]
    public partial int Counter { get; set; }
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        // InjectSubscriptionGenerator emits partial methods
        generatedSource.ShouldContain("__SubscribeToSharedState");
        generatedSource.ShouldContain("__UnsubscribeFromSharedState");
    }

    [Fact]
    public void Multiple_SharedState_Injections()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class ThemeState
{
    [Shared]
    public partial string Theme { get; set; }
}

public partial class ProjectState
{
    [Shared]
    public partial int SelectedId { get; set; }
}

public partial class MyComponent : ComponentBase
{
    [Inject]
    public ThemeState Theme { get; set; } = default!;

    [Inject]
    public ProjectState Project { get; set; } = default!;
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("Theme.StateChanged += __OnSharedStateChanged");
        generatedSource.ShouldContain("Project.StateChanged += __OnSharedStateChanged");
    }

    [Fact]
    public void NonPartial_Class_Skipped()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class ThemeState
{
    [Shared]
    public partial string Theme { get; set; }
}

public class MyComponent : ComponentBase
{
    [Inject]
    public ThemeState Theme { get; set; } = default!;
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        // No subscription generated for non-partial class
        if (generatedSource != null)
        {
            generatedSource.ShouldNotContain("__OnSharedStateChanged");
        }
    }
}
