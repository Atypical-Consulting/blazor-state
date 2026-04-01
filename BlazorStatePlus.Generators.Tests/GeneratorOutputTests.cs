using Microsoft.CodeAnalysis;
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

        Assert.Equal(output1, output2);
        Assert.DoesNotContain(diag1, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
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

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(generatedSource);
        Assert.Contains("CreateSlice", generatedSource);
        Assert.Contains("SliceInitContext", generatedSource);
        Assert.Contains("OnInitializeSlices", generatedSource);
        Assert.Contains("Dispose", generatedSource);
    }
}
