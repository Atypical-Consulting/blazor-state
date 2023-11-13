namespace ObjectCalisthenics;

/// <summary>
/// OC0008: Use first-class collections
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FirstClassCollectionsAnalyzer : DiagnosticAnalyzer
{
    // Prefix all diagnostic IDs with OC (Object Calisthenics)
    public const string DiagnosticId = "OC0008";

    // The title of the diagnostic.
    private static readonly LocalizableString Title
        = CreateLocalizableResourceString(nameof(Resources.OC0008Title));
    
    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat
        = CreateLocalizableResourceString(nameof(Resources.OC0008MessageFormat));

    // The description of the diagnostic.
    private static readonly LocalizableString Description
        = CreateLocalizableResourceString(nameof(Resources.OC0008Description));

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if the class contains a collection type.
        bool containsCollection = false;
        foreach (var member in classDeclaration.Members)
        {
            if (member is FieldDeclarationSyntax fieldDeclaration)
            {
                var isCollectionType = fieldDeclaration.Declaration.Type is GenericNameSyntax genericName &&
                                       genericName.Identifier.Text.EndsWith("Collection");
                containsCollection |= isCollectionType;
            }
        }

        // If contains collection, ensure no other member variables exist.
        if (containsCollection && classDeclaration.Members.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }
}