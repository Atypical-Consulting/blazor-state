using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        ObjectCalisthenics.SampleSemanticAnalyzer>;

namespace ObjectCalisthenics.Tests.Samples;

public class SampleSemanticAnalyzerTests
{
    [Fact]
    public async Task SetSpeedHugeSpeedSpecified_AlertDiagnostic()
    {
        const string text =
            """
            public class Program
            {
                public void Main()
                {
                    var spaceship = new Spaceship();
                    spaceship.SetSpeed(300000000);
                }
            }

            public class Spaceship
            {
                public void SetSpeed(long speed) {}
            }
            """;

        var expected = Verifier.Diagnostic()
            .WithLocation(6, 28)
            .WithArguments("300000000");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}