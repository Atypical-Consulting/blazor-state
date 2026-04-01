using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazorStatePlus.Generators;

[Generator]
internal sealed class SliceIncrementalGenerator : IIncrementalGenerator
{
    private const string SliceAttributeFullName = "BlazorStatePlus.Attributes.SliceAttribute";
    private const string StateSliceFullName = "BlazorStatePlus.Abstractions.IStateSlice`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all fields annotated with [Slice]
        var fieldInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            SliceAttributeFullName,
            predicate: static (node, _) => node is VariableDeclaratorSyntax,
            transform: static (ctx, ct) => ExtractFieldInfo(ctx, ct))
            .Where(static x => x != null);

        // Step 2: Collect all fields and group by class
        var collected = fieldInfos.Collect();

        // Step 3: Combine with compilation and emit
        context.RegisterSourceOutput(collected, Execute);
    }

    private static FieldInfo? ExtractFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // The target node is a VariableDeclaratorSyntax
        if (ctx.TargetNode is not VariableDeclaratorSyntax variableDeclarator)
            return null;

        // Get the field symbol from the semantic model
        var symbol = ctx.TargetSymbol;
        if (symbol is not IFieldSymbol fieldSymbol)
            return null;

        var containingType = fieldSymbol.ContainingType;
        if (containingType == null)
            return null;

        // Get the attribute data
        AttributeData? sliceAttr = null;
        foreach (var attr in ctx.Attributes)
        {
            sliceAttr = attr;
            break;
        }

        if (sliceAttr == null)
            return null;

        // Extract attribute properties
        string? timeToLive = null;
        bool allowUpdatesOnNavigation = false;

        foreach (var namedArg in sliceAttr.NamedArguments)
        {
            if (namedArg is { Key: "TimeToLive", Value.Value: string ttl })
            {
                timeToLive = ttl;
            }
            else if (namedArg is { Key: "AllowUpdatesOnNavigation", Value.Value: bool allow })
            {
                allowUpdatesOnNavigation = allow;
            }
        }

        // Check if the variable declarator has an initializer
        bool hasInitializer = variableDeclarator.Initializer != null;

        return new FieldInfo(
            fieldSymbol: fieldSymbol,
            fieldName: fieldSymbol.Name,
            fieldLocation: fieldSymbol.Locations.FirstOrDefault() ?? Location.None,
            containingClass: containingType,
            timeToLive: timeToLive,
            allowUpdatesOnNavigation: allowUpdatesOnNavigation,
            hasInitializer: hasInitializer);
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<FieldInfo?> fields)
    {
        if (fields.IsDefaultOrEmpty)
            return;

        // Group by containing class
        var grouped = new Dictionary<INamedTypeSymbol, List<FieldInfo>>(SymbolEqualityComparer.Default);
        foreach (var field in fields)
        {
            if (field == null) continue;

            if (!grouped.TryGetValue(field.ContainingClass, out var list))
            {
                list = new List<FieldInfo>();
                grouped[field.ContainingClass] = list;
            }
            list.Add(field);
        }

        foreach (var kvp in grouped)
        {
            ProcessClass(spc, kvp.Key, kvp.Value);
        }
    }

    private static void ProcessClass(SourceProductionContext spc, INamedTypeSymbol classSymbol, List<FieldInfo> fields)
    {
        var className = classSymbol.Name;
        var classLocation = classSymbol.Locations.FirstOrDefault() ?? Location.None;

        // --- Validation ---

        // 1. Check partial
        bool isPartial = false;
        foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is ClassDeclarationSyntax classDecl)
            {
                foreach (var modifier in classDecl.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.PartialKeyword))
                    {
                        isPartial = true;
                        break;
                    }
                }
            }
            if (isPartial) break;
        }

        if (!isPartial)
        {
            foreach (var field in fields)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialClass,
                    field.FieldLocation,
                    field.FieldName,
                    className));
            }
            return; // Can't generate for non-partial
        }

        // 2. Check ComponentBase inheritance
        if (!InheritsFromComponentBase(classSymbol))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NotComponentBase,
                classLocation,
                className));
            return;
        }

        // 3. Validate each field
        var validFields = new List<SliceFieldModel>();
        bool hasErrors = false;

        foreach (var field in fields)
        {
            // Check static
            if (field.FieldSymbol.IsStatic)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticField,
                    field.FieldLocation,
                    field.FieldName));
                hasErrors = true;
                continue;
            }

            // Check IStateSlice<T>
            var fieldType = field.FieldSymbol.Type;
            string? typeArgument = null;
            string? fullTypeArgument = null;

            if (fieldType is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                var originalDef = namedType.OriginalDefinition;
                if (originalDef.ToDisplayString() == "BlazorStatePlus.Abstractions.IStateSlice<T>")
                {
                    var typeArg = namedType.TypeArguments[0];
                    typeArgument = typeArg.Name;
                    fullTypeArgument = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            if (typeArgument == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidFieldType,
                    field.FieldLocation,
                    field.FieldName));
                hasErrors = true;
                continue;
            }

            // Validate TimeToLive
            if (field.TimeToLive != null)
            {
                if (!TimeSpan.TryParse(field.TimeToLive, out _))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidTimeToLive,
                        field.FieldLocation,
                        field.TimeToLive,
                        field.FieldName));
                    hasErrors = true;
                    continue;
                }
            }

            // Warn about initializer
            if (field.HasInitializer)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FieldHasInitializer,
                    field.FieldLocation,
                    field.FieldName));
            }

            // Convert field name to property name
            string propertyName = ConvertFieldNameToPropertyName(field.FieldName);

            // Build base key
            string fieldNameWithoutUnderscore = field.FieldName.TrimStart('_');
            string baseKey = className + "." + fieldNameWithoutUnderscore;

            validFields.Add(new SliceFieldModel
            {
                FieldName = field.FieldName,
                PropertyName = propertyName,
                TypeArgument = typeArgument,
                FullTypeArgument = fullTypeArgument!,
                TimeToLive = field.TimeToLive,
                AllowUpdatesOnNavigation = field.AllowUpdatesOnNavigation,
                BaseKey = baseKey,
                FieldLocation = field.FieldLocation
            });
        }

        if (hasErrors || validFields.Count == 0)
            return;

        // 4. Check duplicate keys
        for (int i = 0; i < validFields.Count; i++)
        {
            for (int j = i + 1; j < validFields.Count; j++)
            {
                if (validFields[i].BaseKey == validFields[j].BaseKey)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateKey,
                        validFields[j].FieldLocation,
                        validFields[i].FieldName,
                        validFields[j].FieldName,
                        validFields[i].BaseKey));
                    return;
                }
            }
        }

        // 5. Detect user-implemented IDisposable (Dispose method directly on the class)
        bool userImplementsDisposable = false;
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IMethodSymbol { Name: "Dispose", Parameters.Length: 0, IsAbstract: false })
            {
                userImplementsDisposable = true;
                break;
            }
        }

        if (userImplementsDisposable)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingDisposable,
                classLocation,
                className));
        }

        // 6. Detect user overrides of OnInitialized
        bool userOverridesOnInitialized = false;
        bool userOverridesOnInitializedAsync = false;
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                if (method is { Name: "OnInitialized", Parameters.Length: 0, IsOverride: true })
                {
                    userOverridesOnInitialized = true;
                }
                else if (method is { Name: "OnInitializedAsync", Parameters.Length: 0, IsOverride: true })
                {
                    userOverridesOnInitializedAsync = true;
                }
            }
        }

        if (userOverridesOnInitialized)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingOnInitialized,
                classLocation,
                className));
        }

        // Build the model
        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : classSymbol.ContainingNamespace.ToDisplayString();

        var model = new ComponentModel
        {
            Namespace = ns,
            ClassName = className,
            Fields = validFields,
            UserImplementsDisposable = userImplementsDisposable,
            UserOverridesOnInitialized = userOverridesOnInitialized,
            UserOverridesOnInitializedAsync = userOverridesOnInitializedAsync,
            ClassLocation = classLocation
        };

        // Emit source
        string source = Emitter.Emit(model);
        spc.AddSource($"{className}.g.cs", source);
    }

    private static bool InheritsFromComponentBase(INamedTypeSymbol classSymbol)
    {
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string ConvertFieldNameToPropertyName(string fieldName)
    {
        // Strip leading underscore
        string name = fieldName;
        if (name.Length > 0 && name[0] == '_')
        {
            name = name.Substring(1);
        }

        // Capitalize first letter
        if (name.Length > 0)
        {
            name = char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        return name;
    }

    /// <summary>
    /// Intermediate data extracted per field during the transform phase.
    /// </summary>
    private sealed class FieldInfo
    {
        public IFieldSymbol FieldSymbol { get; }
        public string FieldName { get; }
        public Location FieldLocation { get; }
        public INamedTypeSymbol ContainingClass { get; }
        public string? TimeToLive { get; }
        public bool AllowUpdatesOnNavigation { get; }
        public bool HasInitializer { get; }

        public FieldInfo(
            IFieldSymbol fieldSymbol,
            string fieldName,
            Location fieldLocation,
            INamedTypeSymbol containingClass,
            string? timeToLive,
            bool allowUpdatesOnNavigation,
            bool hasInitializer)
        {
            FieldSymbol = fieldSymbol;
            FieldName = fieldName;
            FieldLocation = fieldLocation;
            ContainingClass = containingClass;
            TimeToLive = timeToLive;
            AllowUpdatesOnNavigation = allowUpdatesOnNavigation;
            HasInitializer = hasInitializer;
        }
    }
}
