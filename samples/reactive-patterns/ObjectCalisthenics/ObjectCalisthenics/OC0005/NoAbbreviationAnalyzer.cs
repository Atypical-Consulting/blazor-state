using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    private static readonly Dictionary<string, string> _commonAbbreviations = new()
    {
        { "Args", "Arguments" },
        { "Arg", "Argument" },
        { "Btn", "Button" },
        { "Btns", "Buttons" },
        { "Calc", "Calculate" },
        { "Cmd", "Command" },
        { "Cntr", "Counter" },
        { "Cnt", "Count" },
        { "Config", "Configuration" },
        { "Const", "Constant" },
        { "Ctrl", "Control" },
        { "Cur", "Current" },
        { "Db", "Database" },
        { "Del", "Delete" },
        { "Dest", "Destination" },
        { "Dto", "DataTransferObject" },
        { "Enum", "Enumeration" },
        { "Env", "Environment" },
        { "Evt", "Event" },
        { "Ex", "Exception" },
        { "Ext", "Extension" },
        { "Func", "Function" },
        { "Idx", "Index" },
        { "Img", "Image" },
        { "Info", "Information" },
        { "Init", "Initialize" },
        { "Io", "Input/Output" },
        { "Lib", "Library" },
        { "Max", "Maximum" },
        { "Min", "Minimum" },
        { "Msg", "Message" },
        { "Num", "Number" },
        { "Obj", "Object" },
        { "Param", "Parameter" },
        { "Params", "Parameters" },
        { "Proc", "Process" },
        { "Ref", "Reference" },
        { "Req", "Request" },
        { "Resp", "Response" },
        { "Ret", "Return" },
        { "Svc", "Service" },
        { "Src", "Source" },
        { "Str", "String" },
        { "Sys", "System" },
        { "Temp", "Temporary" },
        { "Txt", "Text" },
        { "Ui", "User Interface" },
        { "Util", "Utility" },
        { "Val", "Value" },
        { "Var", "Variable" },
        { "Vec", "Vector" },
        { "Ver", "Version" },
        { "Wd", "Word" },
        { "Wrd", "Word" },
        { "Wnd", "Window" },
    };

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
        var methodName = methodDeclaration.Identifier.ValueText;

        if (!ContainsAbbreviation(methodName))
        {
            return;
        }
        
        var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool ContainsAbbreviation(string methodName)
    {
        // This Regex pattern will match an abbreviation followed by an uppercase letter or the end of the string
        var pattern = string.Join("|", _commonAbbreviations.Keys.Select(abbreviation => $"{abbreviation}(?=[A-Z]|$)"));

        return Regex.IsMatch(methodName, pattern);
    }
}