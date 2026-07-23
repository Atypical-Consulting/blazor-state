namespace ObjectCalisthenics;

/// <summary>
/// OC0009: Do not use getters/setters/properties
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoGettersSettersAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0009";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0009Title));
    
    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0009MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0009Description));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Encapsulation";

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
        context.RegisterSyntaxNodeAction(
            Analyze, SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var accessorDeclaration = (AccessorDeclarationSyntax)context.Node;

        // Report a diagnostic at the location of the accessor declaration
        var diagnostic = Diagnostic.Create(Rule, accessorDeclaration.GetLocation(), accessorDeclaration.Keyword.Text);
        context.ReportDiagnostic(diagnostic);
    }
}