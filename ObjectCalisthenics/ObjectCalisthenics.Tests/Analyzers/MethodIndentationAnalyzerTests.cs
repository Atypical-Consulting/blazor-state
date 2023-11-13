using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.MethodIndentationAnalyzer>;

namespace ObjectCalisthenics.Tests.Analyzers;

public class MethodIndentationAnalyzerTests
{
    [Fact]
    public async Task MethodWithNoIndentation_DoesNotReportDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                void TestMethod()
                {
                    int i = 0;
                    i++;
                }
            }
            """;
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }

    [Fact]
    public async Task MethodWithOneLevelOfIndentation_DoesNotReportDiagnostic()
    {
        // This test code has a method with only one level of indentation
        const string testCode =
            """
            class TestClass
            {
                void TestMethod()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        System.Console.WriteLine(i);
                    }
                }
            }
            """;
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }
    
    [Fact]
    public async Task MethodWithMultipleStatementsAtSameLevel_DoesNotReportDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                void TestMethod()
                {
                    int i = 0;
                    if (i < 10)
                    {
                        i++;
                    }
            
                    if (i > 0)
                    {
                        i--;
                    }
                }
            }
            """;
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }
    
    [Fact]
    public async Task MethodWithTwoLevelsOfIndentationInLoop_ReportsDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                void TestMethod()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i % 2 == 0)
                        {
                            System.Console.WriteLine(i);
                        }
                    }
                }
            }
            """;
        
        var expectedDiagnostic = Verifier
            .Diagnostic("OC0001")
            .WithSpan(3, 10, 3, 20)
            .WithArguments("TestMethod");
        
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for two levels of indentation
    }
    
    [Fact]
    public async Task MethodWithMoreThanOneLevelOfIndentation_ReportsDiagnostic()
    {
        // This test code has a method with two levels of indentation
        const string testCode =
            """
            class TestClass
            {
                void TestMethod()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i == 5)
                        {
                            System.Console.WriteLine("More than one level of indentation");
                        }
                    }
                }
            }
            """;
        var expectedDiagnostic = Verifier
            .Diagnostic("OC0001")
            .WithSpan(3, 10, 3, 20)
            // .WithSpan(6, 13, 8, 63)
            .WithArguments("TestMethod");
        
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
    }
}