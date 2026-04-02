using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class SharedGeneratorTests
{
    [Fact]
    public void Generates_Backing_Field_And_Property_For_Shared()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial decimal Total { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldContain("partial decimal Total");
        generatedSource.ShouldContain("__Total_backing");
        generatedSource.ShouldContain("TotalMeta");
        generatedSource.ShouldContain("INotifyStateChanged");
    }

    [Fact]
    public void Generates_StateChanged_Event()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial int Count { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldContain("StateChanged");
    }

    [Fact]
    public void TBS001_On_NonPartial_Shared_Property()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public int Count { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS001");
    }

    [Fact]
    public void TBS002_On_NonPartial_Class()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public class CartState
{
    [Shared]
    public partial int Count { get; set; }
}";
        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "TBS002");
    }

    [Fact]
    public void Does_Not_Add_Lifecycle_Methods()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial decimal Total { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource.ShouldNotContain("OnInitialized");
        generatedSource.ShouldNotContain("StateManager");
        generatedSource.ShouldNotContain("Dispose");
    }

    [Fact]
    public void Multiple_Shared_Properties()
    {
        var source = @"
using TheBlazorState.Attributes;

namespace Test;

public partial class CartState
{
    [Shared]
    public partial int Count { get; set; }

    [Shared]
    public partial decimal Total { get; set; }
}";
        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);
        diagnostics.ShouldBeEmpty();
        generatedSource.ShouldContain("__Count_backing");
        generatedSource.ShouldContain("__Total_backing");
        generatedSource.ShouldContain("CountMeta");
        generatedSource.ShouldContain("TotalMeta");
    }
}
