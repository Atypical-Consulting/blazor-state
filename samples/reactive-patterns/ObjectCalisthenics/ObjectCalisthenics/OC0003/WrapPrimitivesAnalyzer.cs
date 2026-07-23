namespace ObjectCalisthenics;

/// <summary>
/// OC0003: Wrap all primitives and strings
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WrapPrimitivesAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0003";

    // The title of the diagnostic.
    private static readonly LocalizableResourceString Title 
        = CreateLocalizableResourceString(nameof(Resources.OC0003Title));

    // The message that will be displayed to the user.
    private static readonly LocalizableResourceString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0003MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableResourceString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0003Description));
    
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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.FieldDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Get all the nodes that represent variable declarations.
        var variableDeclarations = context.Node.DescendantNodes().OfType<VariableDeclarationSyntax>();

        foreach (var declaration in variableDeclarations)
        {
            // Check if the variable is of a primitive type or string.
            if (declaration.Type is PredefinedTypeSyntax predefinedType)
            {
                // For each such type, create a diagnostic.
                var diagnostic = Diagnostic.Create(
                    Rule,
                    declaration.GetLocation(),
                    context.ContainingSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            } 
        }
    }
}