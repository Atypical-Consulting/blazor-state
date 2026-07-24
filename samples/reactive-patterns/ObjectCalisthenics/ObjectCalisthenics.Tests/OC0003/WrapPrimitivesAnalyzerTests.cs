using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.WrapPrimitivesAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0003;

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
        
        // WrapPrimitivesAnalyzer reports the whole "Type name = initializer" VariableDeclarationSyntax
        // (declaration.GetLocation() in WrapPrimitivesAnalyzer.Analyze), not just the field identifier,
        // so each expected span has to cover the full declaration text, not "column + fieldName.Length".
        // get 10 diagnostics
        List<DiagnosticResult> diagnosticResults = new();

        AddDiagnosticResult(diagnosticResults, 3, 13, 23, "_a");
        AddDiagnosticResult(diagnosticResults, 4, 13, 32, "_b");
        AddDiagnosticResult(diagnosticResults, 5, 13, 27, "_c");
        AddDiagnosticResult(diagnosticResults, 6, 13, 24, "_d");
        AddDiagnosticResult(diagnosticResults, 7, 13, 26, "_e");
        AddDiagnosticResult(diagnosticResults, 8, 13, 30, "_f");
        AddDiagnosticResult(diagnosticResults, 9, 13, 28, "_g");
        AddDiagnosticResult(diagnosticResults, 10, 13, 28, "_h");
        AddDiagnosticResult(diagnosticResults, 11, 13, 24, "_i");
        AddDiagnosticResult(diagnosticResults, 12, 13, 25, "_j");

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

        // The outer field `_k` is of the custom-wrapped type IntWrapper, so it correctly produces no
        // diagnostic. IntWrapper's own backing field `Value`, however, is a genuine unwrapped `int`
        // field, so WrapPrimitivesAnalyzer correctly flags it too: a wrapper type is not itself exempt
        // from the "wrap all primitives" rule for the fields declared inside it.
        List<DiagnosticResult> diagnosticResults = new()
        {
            Verifier.Diagnostic(WrapPrimitivesAnalyzer.DiagnosticId)
                .WithSpan(8, 13, 8, 22)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("Value"),
        };
        await Verifier.VerifyAnalyzerAsync(testCode, diagnosticResults.ToArray());
    }

    private void AddDiagnosticResult(ICollection<DiagnosticResult> diagnosticResults, int line, int startColumn, int endColumn, string fieldName)
    {
        diagnosticResults.Add(Verifier.Diagnostic(WrapPrimitivesAnalyzer.DiagnosticId)
            .WithSpan("/0/Test0.cs", line, startColumn, line, endColumn)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(fieldName));
    }


}