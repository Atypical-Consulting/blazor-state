using Microsoft.CodeAnalysis;
using Shouldly;
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
        diagnostics.ShouldContain(d => d.Id == "BSP001");
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
        diagnostics.ShouldContain(d => d.Id == "BSP002");
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
        diagnostics.ShouldContain(d => d.Id == "BSP003");
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
        diagnostics.ShouldContain(d => d.Id == "BSP005");
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
        diagnostics.ShouldContain(d => d.Id == "BSP008");
    }

    [Fact]
    public void BSP012_NullForgivingInitializer_NoDiagnostic()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter = null!;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldNotContain(d => d.Id == "BSP012");
    }

    [Fact]
    public void BSP012_RealInitializer_ReportsDiagnostic()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter = default!;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "BSP012");
    }

    [Fact]
    public void BSP006_ExistingDisposable_ReportsWarning()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;
            using System;

            namespace TestApp;

            public partial class MyComponent : ComponentBase, IDisposable
            {
                [Slice]
                private IStateSlice<int> _counter;

                public void Dispose() { }
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "BSP006");
    }

    [Fact]
    public void BSP007_DuplicateKey_ReportsError()
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

                [Slice]
                private IStateSlice<string> _Counter;
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "BSP007");
    }

    [Fact]
    public void BSP011_ExistingOnInitialized_ReportsWarning()
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

                protected override void OnInitialized()
                {
                    base.OnInitialized();
                }
            }
            """;

        var diagnostics = TestHelper.GetDiagnostics(source);
        diagnostics.ShouldContain(d => d.Id == "BSP011");
    }

    [Fact]
    public void BSP001_NonPartialClass_HasSourceLocation()
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
        var bsp001 = diagnostics.First(d => d.Id == "BSP001");
        bsp001.Location.ShouldNotBe(Location.None);
        bsp001.Location.SourceTree.ShouldNotBeNull();
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
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
    }
}
