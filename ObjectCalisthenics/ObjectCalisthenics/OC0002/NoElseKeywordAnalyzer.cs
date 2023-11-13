namespace ObjectCalisthenics;

/// <summary>
/// OC0002: Do not use the `else` keyword
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoElseKeywordAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0002";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0002Title));
    
    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0002MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0002Description));

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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
    }
    
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Cast the context node to a MethodDeclarationSyntax node.
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Check if the method contains any 'else' clauses.
        var elseClauses = methodDeclaration.DescendantNodes()
            .OfType<ElseClauseSyntax>();

        foreach (var elseClause in elseClauses)
        {
            // For each 'else' clause found, create a diagnostic at its location.
            var diagnostic = Diagnostic.Create(
                Rule,
                elseClause.GetLocation(),
                methodDeclaration.Identifier.ValueText);

            context.ReportDiagnostic(diagnostic);
        }
    }

}