namespace ObjectCalisthenics;

/// <summary>
/// OC0005: Do not abbreviate
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoAbbreviationAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0005";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0005Title));
    
    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0005MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0005Description));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Names";

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
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        throw new NotImplementedException();
    }
}