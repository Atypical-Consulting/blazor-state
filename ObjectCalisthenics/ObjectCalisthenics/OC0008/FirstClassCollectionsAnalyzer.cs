using System.Collections.Generic;

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
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
        // context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        var isCollectionType = IsCollectionType(fieldDeclaration.Declaration.Type, context);
        if (isCollectionType)
        {
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    variable.Identifier.GetLocation(),
                    variable.Identifier.Text);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsCollectionType(TypeSyntax typeSyntax, SyntaxNodeAnalysisContext context)
    {
        // Get the type symbol for the type syntax
        var typeSymbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;

        if (typeSymbol == null)
        {
            return false;
        }

        // Check if the type implements IEnumerable, ICollection, or IList
        // You may add more interfaces or specific types as per your requirements
        var collectionTypeNames = new HashSet<string>
        {
            "System.Collections.IEnumerable",
            "System.Collections.Generic.IEnumerable",
            "System.Collections.ICollection",
            "System.Collections.Generic.ICollection",
            "System.Collections.IList",
            "System.Collections.Generic.IList"
        };

        foreach (var symbol in typeSymbol.AllInterfaces)
        {
            if (collectionTypeNames.Contains(symbol.ConstructedFrom.ToString()))
            {
                return true;
            }
        }

        return false;
    }

}