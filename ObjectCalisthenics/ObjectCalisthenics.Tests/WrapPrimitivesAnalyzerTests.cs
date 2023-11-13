using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.WrapPrimitivesAnalyzer>;

namespace ObjectCalisthenics.Tests;

public class WrapPrimitivesAnalyzerTests
{
    [Fact]
    public async Task AnalyzeCodeForPrimitiveWrapping()
    {
        const string testCode =
            """
            public class ExampleOC0003
            {
                private int _a = 5;
                private string _b = "hello";
                private bool _c = true;
                private byte _d = 0;
                private char _e = 'a';
                private decimal _f = 0.0m;
                private double _g = 0.0;
                private float _h = 0.0f;
                private long _i = 0;
                private sbyte _j = 0;
            }
            """;
        
        // get 10 diagnostics
        List<DiagnosticResult> diagnosticResults = new();
        
        AddDiagnosticResult(diagnosticResults, 3, 13, "_a");
        AddDiagnosticResult(diagnosticResults, 4, 13, "_b");
        AddDiagnosticResult(diagnosticResults, 5, 13, "_c");
        AddDiagnosticResult(diagnosticResults, 6, 13, "_d");
        AddDiagnosticResult(diagnosticResults, 7, 13, "_e");
        AddDiagnosticResult(diagnosticResults, 8, 13, "_f");
        AddDiagnosticResult(diagnosticResults, 9, 13, "_g");
        AddDiagnosticResult(diagnosticResults, 10, 13, "_h");
        AddDiagnosticResult(diagnosticResults, 11, 13, "_i");
        AddDiagnosticResult(diagnosticResults, 12, 13, "_j");

        await Verifier.VerifyAnalyzerAsync(testCode, diagnosticResults.ToArray());
    }
    
    
    [Fact]
    public async Task AnalyzeCodeForPrimitiveWrapping_With_CustomType()
    {
        const string testCode =
            """
            public class ExampleOC0003
            {
                private IntWrapper _k = new IntWrapper(5);
            }

            public class IntWrapper
            {
                private int Value;
                
                public IntWrapper(int value)
                {
                    Value = value;
                }
            }
            """;
        
        // get 0 diagnostic
        List<DiagnosticResult> diagnosticResults = new();
        await Verifier.VerifyAnalyzerAsync(testCode, diagnosticResults.ToArray());
    }
    
    private void AddDiagnosticResult(ICollection<DiagnosticResult> diagnosticResults, int line, int column, string fieldName)
    {
        diagnosticResults.Add(Verifier.Diagnostic(WrapPrimitivesAnalyzer.DiagnosticId)
            .WithSpan("/0/Test0.cs", line, column, line, column + fieldName.Length)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(fieldName));
    }
    
    
}