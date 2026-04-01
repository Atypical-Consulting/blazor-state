using Microsoft.CodeAnalysis;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class DiagnosticTests
{
    [Fact]
    public void BSP001_NonPartialClass_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP001");
    }

    [Fact]
    public void BSP002_WrongFieldType_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private int _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP002");
    }

    [Fact]
    public void BSP003_NotComponentBase_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP003");
    }

    [Fact]
    public void BSP005_InvalidTTL_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice(TimeToLive = "not-a-timespan")]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP005");
    }

    [Fact]
    public void BSP008_StaticField_ReportsError()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private static IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "BSP008");
    }

    [Fact]
    public void ValidComponent_NoDiagnosticErrors()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
