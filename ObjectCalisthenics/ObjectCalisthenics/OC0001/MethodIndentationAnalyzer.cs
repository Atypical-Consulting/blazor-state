namespace ObjectCalisthenics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodIndentationAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0001";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0001Title));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0001MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0001Description));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Readability";

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
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var visitor = new IndentationVisitor();
        visitor.Visit(methodDeclaration);

        if (!visitor.HasMultipleLevelsOfIndentation)
        {
            return;
        }
        
        var diagnostic = Diagnostic.Create(
            Rule, 
            methodDeclaration.Identifier.GetLocation(),
            methodDeclaration.Identifier.Text);
        context.ReportDiagnostic(diagnostic);
    }
}