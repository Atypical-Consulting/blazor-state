using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.NoElseKeywordAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0002;

public class NoElseKeywordAnalyzerTests
{
    [Fact]
    public async Task AnalyzeCodeForElseKeyword()
    {
        const string testCode =
            """
            using System;
            
            public class ExampleOC0002
            {
                private void TestMethod()
                {
                    if (true)
                    {
                        Console.WriteLine("Yes, we can.");
                    }
                    // OC0002: 'else' keyword detected in method 'TestMethod'
                    else
                    {
                        Console.WriteLine("No, we can't.");
                    }
                }
            }
            """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0002")
            .WithSpan(12, 9, 15, 10) // The location of the else keyword
            .WithArguments("TestMethod");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for else keyword
    }
    
    [Fact]
    public async Task AnalyzeCodeForElseKeyword_With_ElseIf()
    {
        const string testCode =
            """
            using System;
            
            public class ExampleOC0002
            {
                private void TestMethod()
                {
                    if (true)
                    {
                        Console.WriteLine("Yes, we can.");
                    }
                    // OC0002: 'else' keyword detected in method 'TestMethod'
                    else if (false)
                    {
                        Console.WriteLine("No, we can't.");
                    }
                }
            }
            """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0002")
            .WithSpan(12, 9, 15, 10) // The location of the else keyword
            .WithArguments("TestMethod");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for else keyword
    }
    
    [Fact]
    public async Task AnalyzeCodeForElseKeyword_With_ElseIf_With_Else()
    {
        const string testCode =
            """
            using System;
            
            public class ExampleOC0002
            {
                private void TestMethod()
                {
                    if (true)
                    {
                        Console.WriteLine("Yes, we can.");
                    }
                    // OC0002: 'else' keyword detected in method 'TestMethod'
                    else if (false)
                    {
                        Console.WriteLine("No, we can't.");
                    }
                    else
                    {
                        Console.WriteLine("No, we can't.");
                    }
                }
            }
            """;

        var expectedDiagnostic1 = Verifier
            .Diagnostic("OC0002")
            .WithSpan(12, 9, 19, 10) // The location of the else keyword
            .WithArguments("TestMethod");
        
        var expectedDiagnostic2 = Verifier
            .Diagnostic("OC0002")
            .WithSpan(16, 9, 19, 10) // The location of the else keyword
            .WithArguments("TestMethod");
        
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic1, expectedDiagnostic2); // Expects diagnostic for else keyword
    }
}