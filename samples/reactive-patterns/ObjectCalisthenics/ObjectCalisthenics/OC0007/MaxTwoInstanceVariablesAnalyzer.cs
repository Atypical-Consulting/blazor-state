namespace ObjectCalisthenics;

/// <summary>
/// OC0007: No classes with more than two instance variables
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MaxTwoInstanceVariablesAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0007";

    // The message that will be displayed to the user.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0007Title));
    
    // The description of the diagnostic.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0007MessageFormat));

    // The category of the diagnostic (Design, Naming etc.).
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0007Description));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Complexity";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Retrieve all instance variables (fields that are not static)
        var instanceFields = classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(f => !f.Modifiers.Any(SyntaxKind.StaticKeyword));

        // Count instance variables
        int instanceVariableCount = instanceFields.Sum(f => f.Declaration.Variables.Count);

        if (instanceVariableCount > 2)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.ValueText
            );

            context.ReportDiagnostic(diagnostic);
        }
    }
}