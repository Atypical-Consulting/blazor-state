using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TheBlazorState.Generators;

[Generator]
internal sealed class SliceIncrementalGenerator : IIncrementalGenerator
{
    private const string SliceAttributeFullName = "TheBlazorState.Attributes.SliceAttribute";
    private const string StateSliceFullName = "TheBlazorState.Abstractions.IStateSlice`1";

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

    private static (FieldData Data, FieldLocationData Location)? ExtractFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
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

        foreach (var namedArg in sliceAttr.NamedArguments)
        {
            if (namedArg is { Key: "TimeToLive", Value.Value: string ttl })
            {
                timeToLive = ttl;
            }
        }

        // Check if the variable declarator has an initializer (skip null! which is just CS8618 suppression)
        bool hasInitializer = false;
        if (variableDeclarator.Initializer is EqualsValueClauseSyntax initializer)
        {
            var initValue = initializer.Value;
            if (initValue is PostfixUnaryExpressionSyntax { RawKind: var kind, Operand: LiteralExpressionSyntax literal }
                && kind == (int)SyntaxKind.SuppressNullableWarningExpression
                && literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                hasInitializer = false;
            }
            else
            {
                hasInitializer = true;
            }
        }

        // Extract field type info
        var fieldType = fieldSymbol.Type;
        string fieldTypeDisplayString = fieldType.ToDisplayString();
        string? fieldTypeOriginalDefinitionDisplayString = null;
        string? typeArgumentName = null;
        string? typeArgumentFullyQualified = null;
        bool isGenericType = false;

        if (fieldType is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            isGenericType = true;
            fieldTypeOriginalDefinitionDisplayString = namedType.OriginalDefinition.ToDisplayString();
            if (namedType.TypeArguments.Length > 0)
            {
                var typeArg = namedType.TypeArguments[0];
                typeArgumentName = typeArg.Name;
                typeArgumentFullyQualified = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // Capture locations for diagnostics (excluded from equality)
        var fieldLocation = fieldSymbol.Locations.FirstOrDefault();
        var classLocation = containingType.Locations.FirstOrDefault();

        // Class-level checks (done here while we have symbols)

        // 1. Check partial
        bool isPartialClass = false;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax(ct);
            if (syntax is ClassDeclarationSyntax classDecl)
            {
                foreach (var modifier in classDecl.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.PartialKeyword))
                    {
                        isPartialClass = true;
                        break;
                    }
                }
            }
            if (isPartialClass) break;
        }

        // 2. Check ComponentBase inheritance
        bool inheritsFromComponentBase = false;
        var current = containingType.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
            {
                inheritsFromComponentBase = true;
                break;
            }
            current = current.BaseType;
        }

        // 3. Check user implements IDisposable
        bool userImplementsDisposable = false;
        foreach (var member in containingType.GetMembers())
        {
            if (member is IMethodSymbol { Name: "Dispose", Parameters.Length: 0, IsAbstract: false })
            {
                userImplementsDisposable = true;
                break;
            }
        }

        // 4. Check user overrides OnInitialized / OnInitializedAsync
        bool userOverridesOnInitialized = false;
        bool userOverridesOnInitializedAsync = false;
        foreach (var member in containingType.GetMembers())
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

        // Containing class info
        string containingClassName = containingType.Name;
        string containingClassNamespace = containingType.ContainingNamespace.IsGlobalNamespace
            ? ""
            : containingType.ContainingNamespace.ToDisplayString();

        var fieldData = new FieldData(
            FieldName: fieldSymbol.Name,
            ContainingClassName: containingClassName,
            ContainingClassNamespace: containingClassNamespace,
            FieldTypeDisplayString: fieldTypeDisplayString,
            FieldTypeOriginalDefinitionDisplayString: fieldTypeOriginalDefinitionDisplayString,
            TypeArgumentName: typeArgumentName,
            TypeArgumentFullyQualified: typeArgumentFullyQualified,
            IsStatic: fieldSymbol.IsStatic,
            IsGenericType: isGenericType,
            TimeToLive: timeToLive,
            HasInitializer: hasInitializer,
            IsPartialClass: isPartialClass,
            InheritsFromComponentBase: inheritsFromComponentBase,
            UserImplementsDisposable: userImplementsDisposable,
            UserOverridesOnInitialized: userOverridesOnInitialized,
            UserOverridesOnInitializedAsync: userOverridesOnInitializedAsync);

        var locationData = new FieldLocationData
        {
            FieldLocation = fieldLocation ?? Location.None,
            ClassLocation = classLocation ?? Location.None
        };

        return (fieldData, locationData);
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<(FieldData Data, FieldLocationData Location)?> fields)
    {
        if (fields.IsDefaultOrEmpty)
            return;

        // Group by containing class (using primitives)
        var grouped = new Dictionary<(string ClassName, string Namespace), List<(FieldData Data, FieldLocationData Location)>>();
        foreach (var field in fields)
        {
            if (field == null) continue;

            var key = (field.Value.Data.ContainingClassName, field.Value.Data.ContainingClassNamespace);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<(FieldData Data, FieldLocationData Location)>();
                grouped[key] = list;
            }
            list.Add(field.Value);
        }

        foreach (var kvp in grouped)
        {
            ProcessClass(spc, kvp.Value);
        }
    }

    private static void ProcessClass(SourceProductionContext spc, List<(FieldData Data, FieldLocationData Location)> fields)
    {
        var first = fields[0];
        var className = first.Data.ContainingClassName;
        var ns = first.Data.ContainingClassNamespace;

        // --- Validation ---

        // 1. Check partial (same for all fields in the class)
        if (!first.Data.IsPartialClass)
        {
            foreach (var field in fields)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialClass,
                    field.Location.FieldLocation,
                    field.Data.FieldName,
                    className));
            }
            return; // Can't generate for non-partial
        }

        // 2. Check ComponentBase inheritance
        if (!first.Data.InheritsFromComponentBase)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NotComponentBase,
                first.Location.ClassLocation,
                className));
            return;
        }

        // 3. Validate each field
        var validFields = new List<SliceFieldModel>();
        bool hasErrors = false;

        foreach (var field in fields)
        {
            // Check static
            if (field.Data.IsStatic)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticField,
                    field.Location.FieldLocation,
                    field.Data.FieldName));
                hasErrors = true;
                continue;
            }

            // Check IStateSlice<T>
            string? typeArgument = null;
            string? fullTypeArgument = null;

            if (field.Data.IsGenericType &&
                field.Data.FieldTypeOriginalDefinitionDisplayString == "TheBlazorState.Abstractions.IStateSlice<T>")
            {
                typeArgument = field.Data.TypeArgumentName;
                fullTypeArgument = field.Data.TypeArgumentFullyQualified;
            }

            if (typeArgument == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidFieldType,
                    field.Location.FieldLocation,
                    field.Data.FieldName));
                hasErrors = true;
                continue;
            }

            // Validate TimeToLive
            if (field.Data.TimeToLive != null)
            {
                if (!TimeSpan.TryParse(field.Data.TimeToLive, out _))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidTimeToLive,
                        field.Location.FieldLocation,
                        field.Data.TimeToLive,
                        field.Data.FieldName));
                    hasErrors = true;
                    continue;
                }
            }

            // Warn about initializer
            if (field.Data.HasInitializer)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FieldHasInitializer,
                    field.Location.FieldLocation,
                    field.Data.FieldName));
            }

            // Convert field name to property name
            string propertyName = ConvertFieldNameToPropertyName(field.Data.FieldName);

            // Build base key
            string fieldNameWithoutUnderscore = field.Data.FieldName.TrimStart('_');
            string baseKey = className + "." + fieldNameWithoutUnderscore;

            validFields.Add(new SliceFieldModel
            {
                FieldName = field.Data.FieldName,
                PropertyName = propertyName,
                TypeArgument = typeArgument,
                FullTypeArgument = fullTypeArgument!,
                TimeToLive = field.Data.TimeToLive,
                BaseKey = baseKey,
                FieldLocation = field.Location.FieldLocation
            });
        }

        if (hasErrors || validFields.Count == 0)
            return;

        // 4. Check duplicate keys
        for (int i = 0; i < validFields.Count; i++)
        {
            for (int j = i + 1; j < validFields.Count; j++)
            {
                if (string.Equals(validFields[i].BaseKey, validFields[j].BaseKey, StringComparison.OrdinalIgnoreCase))
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

        // 5. Detect user-implemented IDisposable (pre-extracted)
        bool userImplementsDisposable = first.Data.UserImplementsDisposable;

        if (userImplementsDisposable)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingDisposable,
                first.Location.ClassLocation,
                className));
        }

        // 6. Detect user overrides of OnInitialized (pre-extracted)
        bool userOverridesOnInitialized = first.Data.UserOverridesOnInitialized;
        bool userOverridesOnInitializedAsync = first.Data.UserOverridesOnInitializedAsync;

        if (userOverridesOnInitialized)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingOnInitialized,
                first.Location.ClassLocation,
                className));
        }

        // Build the model
        var model = new ComponentModel
        {
            Namespace = ns,
            ClassName = className,
            Fields = validFields,
            UserImplementsDisposable = userImplementsDisposable,
            UserOverridesOnInitialized = userOverridesOnInitialized,
            UserOverridesOnInitializedAsync = userOverridesOnInitializedAsync,
        };

        // Emit source with namespace-qualified hint name
        string source = Emitter.Emit(model);
        string hintName = string.IsNullOrEmpty(ns)
            ? $"{className}.g.cs"
            : $"{ns}.{className}.g.cs";
        spc.AddSource(hintName, source);
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
    /// Location metadata excluded from equality to avoid poisoning incremental cache.
    /// </summary>
    private readonly struct FieldLocationData
    {
        public Location FieldLocation { get; init; }
        public Location ClassLocation { get; init; }
    }

    private sealed record FieldData(
        string FieldName,
        string ContainingClassName,
        string ContainingClassNamespace,
        string FieldTypeDisplayString,
        string? FieldTypeOriginalDefinitionDisplayString,
        string? TypeArgumentName,
        string? TypeArgumentFullyQualified,
        bool IsStatic,
        bool IsGenericType,
        string? TimeToLive,
        bool HasInitializer,
        bool IsPartialClass,
        bool InheritsFromComponentBase,
        bool UserImplementsDisposable,
        bool UserOverridesOnInitialized,
        bool UserOverridesOnInitializedAsync);
}
