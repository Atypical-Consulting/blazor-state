using System.Linq;
using Shouldly;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<ObjectCalisthenics.OneDotPerLineAnalyzer>;

namespace ObjectCalisthenics.Tests.OC0004;

public class OneDotPerLineAnalyzerTests
{
    [Fact]
    public void IsValidMemberAccess_ShouldReturnTrueForMemberAccessExpression()
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();

        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "TestProject", "TestProject", LanguageNames.CSharp);
        workspace.AddProject(projectInfo);
        var testDocument = workspace.AddDocument(projectId, "Test.cs", SourceText.From("var test = someObject.Property;"));

        var root = testDocument.GetSyntaxRootAsync().Result;
        var memberAccessNode = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();

        // use fluent assertions instead of the above two lines
        OneDotPerLineAnalyzer.IsValidMemberAccess(memberAccessNode, out var memberAccess).ShouldBeTrue();
        memberAccess.ShouldNotBeNull();
    }
    
    [Fact]
    public void CollectMemberAccesses_ShouldCollectAllMemberAccessesIncludingNestedOnes()
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();

        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "TestProject", "TestProject", LanguageNames.CSharp);
        workspace.AddProject(projectInfo);
        var testDocument = workspace.AddDocument(projectId, "Test.cs", SourceText.From("var test = someObject.First().Second.Third();"));

        var root = testDocument.GetSyntaxRootAsync().Result;
        var memberAccessNode = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();

        var memberAccesses = OneDotPerLineAnalyzer.CollectMemberAccesses(memberAccessNode);

        // Assert that all member accesses are collected, including nested ones.
        // Adjust the expected count according to the number of member accesses in your test code.
        memberAccesses.Count().ShouldBe(3);
        memberAccesses[0].ToString().ShouldBe("someObject.First()");
        memberAccesses[1].ToString().ShouldBe("someObject.First().Second");
        memberAccesses[2].ToString().ShouldBe("someObject.First().Second.Third()");
    }

    
    [Theory]
    [InlineData("var node = new TestNode();", false)] // No diagnostics expected
    [InlineData("var node = new TestNode().GetNext();", false)] // No diagnostics expected
    [InlineData("var node = new TestNode().Next;", false)] // No diagnostics expected
    [InlineData("var node = new TestNode().Next.Next;", true, 20, 20, 20, 44, "new TestNode().Next")] // Diagnostic expected for multiple member access with property access
    [InlineData("var node = new TestNode().GetNext().GetNext();", true, 20, 20, 20, 52, "new TestNode().GetNext()")] // Diagnostic expected for multiple member access with method invocation
    [InlineData("var node = new TestNode().GetNext().Next;", true, 20, 20, 20, 49, "new TestNode().GetNext()")] // Diagnostic expected for multiple member access with method invocation and property access
    [InlineData("var node = new TestNode().Next.GetNext();", true, 20, 20, 20, 47, "new TestNode().Next")] // Diagnostic expected for multiple member access with property access and method invocation
    public async Task AnalyzeCodeForMemberAccess(string codeToInsert, bool expectsDiagnostic, int lineStart = 0, int charStart = 0, int lineEnd = 0, int charEnd = 0, string? arguments = null)
    {
        var testCode = CreateTestCode(codeToInsert);

        if (expectsDiagnostic)
        {
            var expectedDiagnostic = Verifier.Diagnostic("OC0004")
                .WithSpan(lineStart, charStart, lineEnd, charEnd)
                .WithArguments(arguments ?? string.Empty);

            await Verifier.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
        }
        else
        {
            await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
        }
    }

    [Fact]
    public async Task MultipleMemberAccessOnMultipleLines_DoesNotReportDiagnostic()
    {
        const string codeToInsert = 
            """
            var node = new TestNode()
                .GetNext()
                .GetNext();
            """;
        
        var testCode = CreateTestCode(codeToInsert);
        await Verifier.VerifyAnalyzerAsync(testCode); // No diagnostics expected
    }

    private static string CreateTestCode(string testMethodBody)
        => $$"""
             class TestNode
             {
                 public TestNode Next { get; }
                 
                 public TestNode()
                 {
                     Next = new TestNode();
                 }
                 
                 public TestNode GetNext()
                 {
                     return Next;
                 }
             }

             class TestClass
             {
                 void TestMethod()
                 {
                     {{testMethodBody}}
                 }
             }
             """;
}
