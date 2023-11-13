using Microsoft.CodeAnalysis.Text;

namespace ObjectCalisthenics;

/// <summary>
/// OC0006: Keep all entities small
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SmallEntitiesAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0006";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0006Title));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0006MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0006Description));

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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Calculate the number of lines in the class
        var lastToken = classDeclaration.GetLastToken();
        var firstToken = classDeclaration.GetFirstToken();
        var lineSpan = Location.Create(classDeclaration.SyntaxTree, TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End)).GetLineSpan();

        // Lines are zero-based, so add 1 for the actual count
        var numberOfLines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

        if (numberOfLines <= 50)
        {
            return;
        }
        
        var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text);
        context.ReportDiagnostic(diagnostic);
    }
}