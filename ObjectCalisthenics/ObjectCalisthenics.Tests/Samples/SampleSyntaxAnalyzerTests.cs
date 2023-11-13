using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        ObjectCalisthenics.SampleSyntaxAnalyzer>;

namespace ObjectCalisthenics.Tests.Samples;

public class SampleSyntaxAnalyzerTests
{
    [Fact]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text =
            """
            public class MyCompanyClass
            {
            }
            """;

        var expected = Verifier.Diagnostic()
            .WithLocation(1, 14)
            .WithArguments("MyCompanyClass");
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}