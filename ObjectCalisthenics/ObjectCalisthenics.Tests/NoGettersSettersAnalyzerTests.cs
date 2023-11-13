using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.NoGettersSettersAnalyzer>;

namespace ObjectCalisthenics.Tests;

public class NoGettersSettersAnalyzerTests
{
    [Fact]
    public async Task ClassWithNoProperties_DoesNotReportDiagnostic()
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
    public async Task ClassWithGetter_ReportsDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                private int _field;
                public int Property
                {
                    get { return _field; }
                }
            }
            """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0009")
            .WithSpan(6, 9, 6, 31) // The location of the getter
            .WithArguments("get");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for getter
    }

    [Fact]
    public async Task ClassWithSetter_ReportsDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                private int _field;
                public int Property
                {
                    set { _field = value; }
                }
            }
            """;

        var expectedDiagnostic = Verifier
            .Diagnostic("OC0009")
            .WithSpan(6, 9, 6, 32) // The location of the setter
            .WithArguments("set");

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic); // Expects diagnostic for setter
    }

    [Fact]
    public async Task ClassWithGetterAndSetter_ReportsDiagnostics()
    {
        const string testCode =
            """
            class TestClass
            {
                private int _field;
                public int Property
                {
                    get { return _field; }
                    set { _field = value; }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            Verifier.Diagnostic("OC0009")
                .WithSpan(6, 9, 6, 31)
                .WithArguments("get"), // The location of the getter
            
            Verifier.Diagnostic("OC0009")
                .WithSpan(7, 9, 7, 32)
                .WithArguments("set")  // The location of the setter
        };

        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostics); // Expects diagnostics for both getter and setter
    }

    [Fact]
    public async Task ClassWithAutoProperty_ReportsDiagnostic()
    {
        const string testCode =
            """
            class TestClass
            {
                public int AutoProperty { get; set; }
            }
            """;

        var expectedDiagnostics = new[]
        {
            Verifier.Diagnostic("OC0009")
                .WithSpan(3, 31, 3, 35)
                .WithArguments("get"), // The location of the getter
            
            Verifier.Diagnostic("OC0009")
                .WithSpan(3, 36, 3, 40)
                .WithArguments("set")  // The location of the setter
        };
        
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostics); // Expects diagnostics for auto-property
    }
}
