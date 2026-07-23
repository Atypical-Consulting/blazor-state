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
        var (diagnostics1, generated) = TestHelper.RunGenerator(source);
        diagnostics1.ShouldBeEmpty();
        var generatedSource = generated!;
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
        var (_, generatedSource) = TestHelper.RunGenerator(source);
        generatedSource!.ShouldContain("StateChanged");
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
        var (_, generated2) = TestHelper.RunGenerator(source);
        var generatedSource2 = generated2!;
        generatedSource2.ShouldNotContain("OnInitialized");
        generatedSource2.ShouldNotContain("StateManager");
        generatedSource2.ShouldNotContain("Dispose");
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
        var (diagnostics2, generated3) = TestHelper.RunGenerator(source);
        diagnostics2.ShouldBeEmpty();
        var generatedSource3 = generated3!;
        generatedSource3.ShouldContain("__Count_backing");
        generatedSource3.ShouldContain("__Total_backing");
        generatedSource3.ShouldContain("CountMeta");
        generatedSource3.ShouldContain("TotalMeta");
    }
}
