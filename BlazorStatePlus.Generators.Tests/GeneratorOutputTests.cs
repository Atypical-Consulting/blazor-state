using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class GeneratorOutputTests
{
    [Fact]
    public void Generator_ProducesDeterministicOutput()
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

        var (diag1, output1) = TestHelper.RunGenerator(source);
        var (diag2, output2) = TestHelper.RunGenerator(source);

        output1.ShouldBe(output2);
        diag1.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void SimpleSlice_GeneratesPartialClass()
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

        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("CreateSlice");
        generatedSource.ShouldContain("SliceInitContext");
        generatedSource.ShouldContain("OnInitializeSlices");
        generatedSource.ShouldContain("Dispose");
    }

    [Fact]
    public void UserOverridesOnInitializedAsync_EmitsPartialHook()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            namespace TestApp;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;

                protected override async Task OnInitializedAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("OnAfterSlicesInitializedAsync");
        generatedSource.ShouldContain("base.OnInitializedAsync()");
    }

    [Fact]
    public void NoUserOverride_OnInitializedAsync_CallsBase()
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

        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldContain("await base.OnInitializedAsync()");
    }

    [Fact]
    public void GlobalNamespace_GeneratesValidCode()
    {
        var source = """
            using BlazorStatePlus.Abstractions;
            using BlazorStatePlus.Attributes;
            using Microsoft.AspNetCore.Components;

            public partial class MyComponent : ComponentBase
            {
                [Slice]
                private IStateSlice<int> _counter;
            }
            """;

        var (diagnostics, generatedSource) = TestHelper.RunGenerator(source);

        generatedSource.ShouldNotBeNull();
        generatedSource.ShouldNotContain("namespace ;");
    }
}
