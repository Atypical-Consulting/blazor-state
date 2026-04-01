using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "BlazorStatePlus";

    public static readonly DiagnosticDescriptor NonPartialClass = new(
        "BSP001",
        "[Slice] requires partial class",
        "Field '{0}' has [Slice] but class '{1}' is not partial",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFieldType = new(
        "BSP002",
        "[Slice] field must be IStateSlice<T>",
        "Field '{0}' has [Slice] but its type is not IStateSlice<T>",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotComponentBase = new(
        "BSP003",
        "[Slice] class must inherit ComponentBase",
        "Class '{0}' has [Slice] fields but does not inherit from ComponentBase",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrphanInitMethod = new(
        "BSP004",
        "OnInitializeSlices without [Slice] fields",
        "Class '{0}' defines OnInitializeSlices but has no [Slice] fields",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTimeToLive = new(
        "BSP005",
        "Invalid TimeToLive format",
        "TimeToLive '{0}' on field '{1}' is not a valid TimeSpan string",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExistingDisposable = new(
        "BSP006",
        "Component already implements IDisposable",
        "Class '{0}' implements IDisposable; call __DisposeSlices() from your Dispose method",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateKey = new(
        "BSP007",
        "Duplicate slice key",
        "Fields '{0}' and '{1}' resolve to the same key '{2}'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticField = new(
        "BSP008",
        "[Slice] on static field",
        "Field '{0}' is static; [Slice] fields must be instance fields",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotField = new(
        "BSP009",
        "[Slice] on property instead of field",
        "'{0}' is a property; [Slice] must be applied to fields",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidInitSignature = new(
        "BSP010",
        "OnInitializeSlices has wrong signature",
        "OnInitializeSlices in '{0}' must be 'partial void OnInitializeSlices(SliceInitContext ctx)'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExistingOnInitialized = new(
        "BSP011",
        "Component overrides OnInitialized",
        "Class '{0}' overrides OnInitialized; use OnAfterSlicesCreated() instead",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldHasInitializer = new(
        "BSP012",
        "[Slice] field has initializer",
        "Field '{0}' has an initializer that will be overwritten by the generator",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSerializable = new(
        "BSP013",
        "Slice type not JSON-serializable",
        "Type '{0}' on field '{1}' may not be JSON-serializable",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
