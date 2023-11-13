using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.SmallEntitiesAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0006;

public class SmallEntitiesAnalyzerTests
{
    [Fact]
    public async Task SmallClass_DoesNotReportDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                void Method1() {}
                void Method2() {}
                // ... Add methods but keep the total count below 50 lines
            }
            """;
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }

    [Fact]
    public async Task LargeClass_ReportsDiagnostic()
    {
        var testCode =
            $$"""
              class TestClass
              {
                  // Simulating a large class with more than 50 lines
                  {{new string('\n', 51)}}
              }
              """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0006")
            .WithSpan(1, 7, 1, 16) // The location of the class name
            .WithArguments("TestClass");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for large class
    }
}