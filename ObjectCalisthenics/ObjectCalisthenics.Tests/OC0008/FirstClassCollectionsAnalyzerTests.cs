using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.FirstClassCollectionsAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0008;

public class FirstClassCollectionsAnalyzerTests
{
    [Fact]
    public async Task AnalyzeCodeForFirstClassCollections()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            
            public class ExampleOC0008
            {
                private List<int> _a = new List<int>();
                private Dictionary<int, string> _b = new Dictionary<int, string>();
                private HashSet<int> _c = new HashSet<int>();
                private Queue<int> _d = new Queue<int>();
                private Stack<int> _e = new Stack<int>();
                private LinkedList<int> _f = new LinkedList<int>();
                private SortedList<int, string> _g = new SortedList<int, string>();
                private SortedSet<int> _h = new SortedSet<int>();
                private SortedDictionary<int, string> _i = new SortedDictionary<int, string>();
            }
            """;

        // get 9 diagnostics
        List<DiagnosticResult> diagnosticResults = new();

        AddDiagnosticResult(diagnosticResults, 5, 23, "_a");
        AddDiagnosticResult(diagnosticResults, 6, 37, "_b");
        AddDiagnosticResult(diagnosticResults, 7, 26, "_c");
        AddDiagnosticResult(diagnosticResults, 8, 24, "_d");
        AddDiagnosticResult(diagnosticResults, 9, 24, "_e");
        AddDiagnosticResult(diagnosticResults, 10, 29, "_f");
        AddDiagnosticResult(diagnosticResults, 11, 37, "_g");
        AddDiagnosticResult(diagnosticResults, 12, 28, "_h");
        AddDiagnosticResult(diagnosticResults, 13, 43, "_i");

        await Verifier.VerifyAnalyzerAsync(testCode, diagnosticResults.ToArray());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("string")]
    [InlineData("object")]
    [InlineData("bool")]
    public async Task AnalyzeCodeForNonFirstClassCollections(string type)
    {
        string testCode =
          $$"""
            using System.Collections.Generic;

            public class ExampleOC0008
            {
                private List<{{type}}> _a = new();
            }
            """;
        
        DiagnosticResult expectedDiagnostic = Verifier
            .Diagnostic("OC0008")
            .WithSpan(5, 20 + type.Length, 5, 22 + type.Length) // The location of the field name
            .WithArguments("_a");
        
        await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
    }
    
    [Fact]
    public async Task AnalyzeCodeForString()
    {
        const string testCode =
            """
            public class ExampleOC0008
            {
                private string _a = "Hello World";
            }
            """;
        
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }
    
    [Fact]
    public async Task AnalyzeCodeForNonCollection()
    {
        const string testCode =
            """
            public class ExampleOC0008
            {
                private int _a = 42;
            }
            """;
        
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }
    
    [Fact]
    public async Task AnalyzeCodeForNonCollectionWithCollectionName()
    {
        const string testCode =
            """
            public class ExampleOC0008
            {
                private int _list = 42;
            }
            """;
        
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }

    private static void AddDiagnosticResult(List<DiagnosticResult> diagnosticResults, int line, int column, string fieldName)
    {
        diagnosticResults.Add(
            Verifier
                .Diagnostic("OC0008")
                .WithSpan(line, column, line, column + fieldName.Length) // The location of the field name
                .WithArguments(fieldName)
        );
    }
}