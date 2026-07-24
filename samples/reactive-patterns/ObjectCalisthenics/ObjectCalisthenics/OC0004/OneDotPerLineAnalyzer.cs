using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace ObjectCalisthenics;

/// <summary>
/// OC0004: Use only one dot per line
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OneDotPerLineAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0004";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0004Title));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0004MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0004Description));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Size";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (!IsValidMemberAccess(context.Node, out var memberAccess) || memberAccess == null)
        {
            return;
        }

        var memberAccesses = CollectMemberAccesses(memberAccess);

        if (memberAccesses.Count <= 1)
        {
            return;
        }

        var (rootMemberAccess, lastMemberAccess) = DetermineRootAndLastMemberAccess(memberAccesses);

        // A chain that is already split across multiple lines (one member access per line) already
        // satisfies the "one dot per line" rule, so it should not be flagged - only chains that pack
        // more than one dot onto a single line are a violation.
        if (SpansMultipleLines(memberAccess.SyntaxTree, rootMemberAccess, lastMemberAccess))
        {
            return;
        }

        var fullExpressionSpan = CalculateFullExpressionSpan(rootMemberAccess, lastMemberAccess);
        var violatingExpressionNode = FindViolatingExpressionNode(memberAccess.SyntaxTree, fullExpressionSpan);

        ReportDiagnostic(context, rootMemberAccess, violatingExpressionNode);
    }

    public static bool IsValidMemberAccess(SyntaxNode node, out MemberAccessExpressionSyntax? memberAccess)
    {
        memberAccess = node as MemberAccessExpressionSyntax;
        return memberAccess != null;
    }

    public static List<MemberAccessExpressionSyntax> CollectMemberAccesses(MemberAccessExpressionSyntax memberAccess)
        => memberAccess
            .DescendantNodesAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .ToList();

    private static (MemberAccessExpressionSyntax Root, MemberAccessExpressionSyntax Last)
        DetermineRootAndLastMemberAccess(List<MemberAccessExpressionSyntax> memberAccesses)
    {
        var rootMemberAccess = memberAccesses.First(ma => ma.Parent is not MemberAccessExpressionSyntax);
        var lastMemberAccess = memberAccesses.Last();
        return (rootMemberAccess, lastMemberAccess);
    }

    private static bool SpansMultipleLines(SyntaxTree syntaxTree, MemberAccessExpressionSyntax rootMemberAccess,
        MemberAccessExpressionSyntax lastMemberAccess)
    {
        var startLine = syntaxTree.GetLineSpan(rootMemberAccess.Span).StartLinePosition.Line;
        var endLine = syntaxTree.GetLineSpan(lastMemberAccess.Span).EndLinePosition.Line;
        return startLine != endLine;
    }

    private static TextSpan CalculateFullExpressionSpan(MemberAccessExpressionSyntax rootMemberAccess,
        MemberAccessExpressionSyntax lastMemberAccess)
    {
        // If the innermost member access in the chain is itself being invoked (e.g. "...GetNext()"),
        // include the call's argument list so the reported expression text is valid, readable code
        // rather than a dangling "GetNext" with no parentheses.
        var end = lastMemberAccess.Parent is InvocationExpressionSyntax invocation
            ? invocation.Span.End
            : lastMemberAccess.Span.End;

        return TextSpan.FromBounds(rootMemberAccess.SpanStart, end);
    }

    private static SyntaxNode FindViolatingExpressionNode(SyntaxTree syntaxTree, TextSpan fullExpressionSpan)
    {
        return syntaxTree.GetRoot().FindNode(fullExpressionSpan);
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax rootMemberAccess, SyntaxNode violatingExpressionNode)
    {
        var diagnostic =
            Diagnostic.Create(Rule, rootMemberAccess.GetLocation(), violatingExpressionNode.ToString().Trim());
        context.ReportDiagnostic(diagnostic);
    }
}