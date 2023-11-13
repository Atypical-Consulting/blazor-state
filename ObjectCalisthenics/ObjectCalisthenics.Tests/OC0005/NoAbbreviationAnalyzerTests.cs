using Microsoft.CodeAnalysis.Testing;
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
        var testCode = CreateTestCode(methodName);
        var expectedDiagnostic = CreateExpectedDiagnostic(methodName);
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
    }
    
    [Theory]
    [InlineData("ExecuteCommand")]
    [InlineData("ParseArguments")]
    [InlineData("ParseArgument")]
    [InlineData("ButtonClick")]
    [InlineData("GetVersion")]
    public async Task AnalyzeCodeForNoAbbreviation(string methodName)
    {
        var testCode = CreateTestCode(methodName);
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }
    
    private static string CreateTestCode(string methodName)
        => $$"""
             public class ExampleOC0005
             {
                 private void {{methodName}}()
                 {
                 }
             }
             """;

    private static DiagnosticResult CreateExpectedDiagnostic(string methodName)
        => Verifier
            .Diagnostic("OC0005")
            .WithSpan(3, 18, 3, 18 + methodName.Length)
            .WithArguments(methodName);
}