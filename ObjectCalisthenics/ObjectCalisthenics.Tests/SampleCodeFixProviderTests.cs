using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<ObjectCalisthenics.SampleSyntaxAnalyzer,
        ObjectCalisthenics.SampleCodeFixProvider>;

namespace ObjectCalisthenics.Tests;

public class SampleCodeFixProviderTests
{
    [Fact]
    public async Task ClassWithMyCompanyTitle_ReplaceWithCommonKeyword()
    {
        const string text =
            """
            public class MyCompanyClass
            {
            }
            """;

        const string newText =
            """
            public class CommonClass
            {
            }
            """;

        var expected = Verifier.Diagnostic()
            .WithLocation(2, 14)
            .WithArguments("MyCompanyClass");
        await Verifier.VerifyCodeFixAsync(text, expected, newText).ConfigureAwait(false);
    }
}