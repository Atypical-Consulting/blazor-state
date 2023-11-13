using Microsoft.CodeAnalysis;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.MaxTwoInstanceVariablesAnalyzer>;

namespace ObjectCalisthenics.Tests.Analyzers;

public class MaxTwoInstanceVariablesAnalyzerTests
{
    [Fact]
    public async Task Analyzer_Should_Not_Report_Diagnostic_When_Two_Or_Fewer_Instance_Variables()
    {
        const string testCode =
            """
            public class TestClass
            {
                private int instanceVariable1;
                private int instanceVariable2;
            }
            """;

        await Verifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task Analyzer_Should_Report_Diagnostic_When_More_Than_Two_Instance_Variables()
    {
        const string testCode =
            """
            public class TestClass
            {
                private int instanceVariable1;
                private int instanceVariable2;
                private int instanceVariable3;
            }
            """;

        var expectedDiagnostic = Verifier.Diagnostic(MaxTwoInstanceVariablesAnalyzer.DiagnosticId)
            .WithSpan(1, 14, 1, 23)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("TestClass");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
    }
    
    [Fact]
    public async Task Analyzer_Should_Not_Report_Diagnostic_When_Two_Or_Fewer_Instance_Variables_With_Static_Variables()
    {
        const string testCode =
            """
            public class TestClass
            {
                private static int staticVariable1;
                private static int staticVariable2;
                private int instanceVariable1;
                private int instanceVariable2;
            }
            """;

        await Verifier.VerifyAnalyzerAsync(testCode);
    }
}