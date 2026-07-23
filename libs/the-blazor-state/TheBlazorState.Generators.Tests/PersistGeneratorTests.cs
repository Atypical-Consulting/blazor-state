using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class PersistGeneratorTests
{
    [Fact]
    public void Generates_Backing_Field_And_Property()
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
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("__Counter_backing");
        generatedSource.ShouldContain("public partial int Counter");
        generatedSource.ShouldContain("CounterMeta");
    }

    [Fact]
    public void Generates_StateContext_With_PropertyConfigurator()
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
        generatedSource.ShouldContain("__StateContext");
        generatedSource.ShouldContain("PropertyConfigurator<int> Counter");
        generatedSource.ShouldContain("ConfigureState");
    }

    [Fact]
    public void Generates_OnInitialized_With_RestoreProperty()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public partial string? Name { get; set; }
}";
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("OnInitialized");
        generatedSource.ShouldContain("RestoreProperty");
        generatedSource.ShouldContain("__stateManager");
    }

    [Fact]
    public void Generates_Dispose()
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
        generatedSource.ShouldContain("public void Dispose()");
        generatedSource.ShouldContain("ClearHandlers");
    }

    [Fact]
    public void TBS001_NonPartialProperty()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS001");
    }

    [Fact]
    public void TBS002_NonPartialClass()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public class MyComponent : ComponentBase
{
    [Persist]
    public partial int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS002");
    }

    [Fact]
    public void TBS004_InvalidTimeToLive()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist(TimeToLive = ""not-valid"")]
    public partial int Counter { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS004");
    }

    [Fact]
    public void No_Diagnostic_On_Valid_Persist()
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
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Generates_TimeToLive_In_StateMeta()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist(TimeToLive = ""00:05:00"")]
    public partial int Counter { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("TimeSpan.Parse");
        generatedSource.ShouldContain("00:05:00");
    }

    [Fact]
    public void Generates_Multiple_Properties()
    {
        var source = @"
using TheBlazorState.Attributes;
using Microsoft.AspNetCore.Components;

namespace Test;

public partial class MyComponent : ComponentBase
{
    [Persist]
    public partial int Counter { get; set; }

    [Persist]
    public partial string? Name { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("__Counter_backing");
        generatedSource.ShouldContain("__Name_backing");
        generatedSource.ShouldContain("__Counter_meta");
        generatedSource.ShouldContain("__Name_meta");
    }

    [Fact]
    public void Generates_OnInitializedAsync_With_AsyncFactory()
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
        generatedSource.ShouldContain("OnInitializedAsync");
        generatedSource.ShouldContain("HasAsyncFactory");
        generatedSource.ShouldContain("InvokeFactoryAsync");
    }
}
