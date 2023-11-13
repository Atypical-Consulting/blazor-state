using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.NoAbbreviationAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0005;

public class NoAbbreviationAnalyzerTests
{
    [Theory]
    [InlineData("ExecuteCmd")]
    [InlineData("ParseArgs")]
    [InlineData("ParseArg")]
    [InlineData("BtnClick")]
    [InlineData("GetVer")]
    public async Task AnalyzeCodeForAbbreviation(string methodName)
    {
        var testCode =
            $$"""
              public class ExampleOC0005
              {
                  private void {{methodName}}()
                  {
                      // OC0005: Abbreviation detected in method 'ExecuteCmd'
                      var a = 5;
                  }
              }
              """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0005")
            .WithSpan(3, 18, 3, 18 + methodName.Length) // The location of the abbreviation
            .WithArguments(methodName);

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for abbreviation
    }
}