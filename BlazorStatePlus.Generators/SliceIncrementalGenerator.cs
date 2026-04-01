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

    private static FieldData? ExtractFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
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

        // Field location
        var fieldLoc = fieldSymbol.Locations.FirstOrDefault();
        string? filePath = fieldLoc?.SourceTree?.FilePath;
        int spanStart = fieldLoc?.SourceSpan.Start ?? 0;
        int spanLength = fieldLoc?.SourceSpan.Length ?? 0;

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

        // Class location
        var classLoc = containingType.Locations.FirstOrDefault();
        string? classFilePath = classLoc?.SourceTree?.FilePath;
        int classSpanStart = classLoc?.SourceSpan.Start ?? 0;
        int classSpanLength = classLoc?.SourceSpan.Length ?? 0;

        // Containing class info
        string containingClassName = containingType.Name;
        string containingClassNamespace = containingType.ContainingNamespace.IsGlobalNamespace
            ? ""
            : containingType.ContainingNamespace.ToDisplayString();

        return new FieldData(
            fieldName: fieldSymbol.Name,
            containingClassName: containingClassName,
            containingClassNamespace: containingClassNamespace,
            fieldTypeDisplayString: fieldTypeDisplayString,
            fieldTypeOriginalDefinitionDisplayString: fieldTypeOriginalDefinitionDisplayString,
            typeArgumentName: typeArgumentName,
            typeArgumentFullyQualified: typeArgumentFullyQualified,
            isStatic: fieldSymbol.IsStatic,
            isGenericType: isGenericType,
            timeToLive: timeToLive,
            hasInitializer: hasInitializer,
            filePath: filePath,
            spanStart: spanStart,
            spanLength: spanLength,
            isPartialClass: isPartialClass,
            inheritsFromComponentBase: inheritsFromComponentBase,
            userImplementsDisposable: userImplementsDisposable,
            userOverridesOnInitialized: userOverridesOnInitialized,
            userOverridesOnInitializedAsync: userOverridesOnInitializedAsync,
            classFilePath: classFilePath,
            classSpanStart: classSpanStart,
            classSpanLength: classSpanLength);
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<FieldData?> fields)
    {
        if (fields.IsDefaultOrEmpty)
            return;

        // Group by containing class (using primitives)
        var grouped = new Dictionary<(string ClassName, string Namespace), List<FieldData>>();
        foreach (var field in fields)
        {
            if (field == null) continue;

            var key = (field.ContainingClassName, field.ContainingClassNamespace);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<FieldData>();
                grouped[key] = list;
            }
            list.Add(field);
        }

        foreach (var kvp in grouped)
        {
            ProcessClass(spc, kvp.Value);
        }
    }

    private static void ProcessClass(SourceProductionContext spc, List<FieldData> fields)
    {
        var first = fields[0];
        var className = first.ContainingClassName;
        var ns = first.ContainingClassNamespace;

        // --- Validation ---

        // 1. Check partial (same for all fields in the class)
        if (!first.IsPartialClass)
        {
            foreach (var field in fields)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NonPartialClass,
                    Location.None,
                    field.FieldName,
                    className));
            }
            return; // Can't generate for non-partial
        }

        // 2. Check ComponentBase inheritance
        if (!first.InheritsFromComponentBase)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NotComponentBase,
                Location.None,
                className));
            return;
        }

        // 3. Validate each field
        var validFields = new List<SliceFieldModel>();
        bool hasErrors = false;

        foreach (var field in fields)
        {
            // Check static
            if (field.IsStatic)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StaticField,
                    Location.None,
                    field.FieldName));
                hasErrors = true;
                continue;
            }

            // Check IStateSlice<T>
            string? typeArgument = null;
            string? fullTypeArgument = null;

            if (field.IsGenericType &&
                field.FieldTypeOriginalDefinitionDisplayString == "BlazorStatePlus.Abstractions.IStateSlice<T>")
            {
                typeArgument = field.TypeArgumentName;
                fullTypeArgument = field.TypeArgumentFullyQualified;
            }

            if (typeArgument == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidFieldType,
                    Location.None,
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
                        Location.None,
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
                    Location.None,
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
                BaseKey = baseKey,
                FieldLocation = Location.None
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

        // 5. Detect user-implemented IDisposable (pre-extracted)
        bool userImplementsDisposable = first.UserImplementsDisposable;

        if (userImplementsDisposable)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingDisposable,
                Location.None,
                className));
        }

        // 6. Detect user overrides of OnInitialized (pre-extracted)
        bool userOverridesOnInitialized = first.UserOverridesOnInitialized;
        bool userOverridesOnInitializedAsync = first.UserOverridesOnInitializedAsync;

        if (userOverridesOnInitialized)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExistingOnInitialized,
                Location.None,
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
            ClassLocation = Location.None
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
    /// Value-equatable data class containing only primitives and strings — no Roslyn symbols.
    /// This ensures the incremental pipeline's caching works correctly across phases.
    /// </summary>
    private sealed class FieldData : IEquatable<FieldData>
    {
        public string FieldName { get; }
        public string ContainingClassName { get; }
        public string ContainingClassNamespace { get; }
        public string FieldTypeDisplayString { get; }
        public string? FieldTypeOriginalDefinitionDisplayString { get; }
        public string? TypeArgumentName { get; }
        public string? TypeArgumentFullyQualified { get; }
        public bool IsStatic { get; }
        public bool IsGenericType { get; }
        public string? TimeToLive { get; }
        public bool HasInitializer { get; }
        public string? FilePath { get; }
        public int SpanStart { get; }
        public int SpanLength { get; }
        // Class-level data (same for all fields in same class)
        public bool IsPartialClass { get; }
        public bool InheritsFromComponentBase { get; }
        public bool UserImplementsDisposable { get; }
        public bool UserOverridesOnInitialized { get; }
        public bool UserOverridesOnInitializedAsync { get; }
        public string? ClassFilePath { get; }
        public int ClassSpanStart { get; }
        public int ClassSpanLength { get; }

        public FieldData(
            string fieldName,
            string containingClassName,
            string containingClassNamespace,
            string fieldTypeDisplayString,
            string? fieldTypeOriginalDefinitionDisplayString,
            string? typeArgumentName,
            string? typeArgumentFullyQualified,
            bool isStatic,
            bool isGenericType,
            string? timeToLive,
            bool hasInitializer,
            string? filePath,
            int spanStart,
            int spanLength,
            bool isPartialClass,
            bool inheritsFromComponentBase,
            bool userImplementsDisposable,
            bool userOverridesOnInitialized,
            bool userOverridesOnInitializedAsync,
            string? classFilePath,
            int classSpanStart,
            int classSpanLength)
        {
            FieldName = fieldName;
            ContainingClassName = containingClassName;
            ContainingClassNamespace = containingClassNamespace;
            FieldTypeDisplayString = fieldTypeDisplayString;
            FieldTypeOriginalDefinitionDisplayString = fieldTypeOriginalDefinitionDisplayString;
            TypeArgumentName = typeArgumentName;
            TypeArgumentFullyQualified = typeArgumentFullyQualified;
            IsStatic = isStatic;
            IsGenericType = isGenericType;
            TimeToLive = timeToLive;
            HasInitializer = hasInitializer;
            FilePath = filePath;
            SpanStart = spanStart;
            SpanLength = spanLength;
            IsPartialClass = isPartialClass;
            InheritsFromComponentBase = inheritsFromComponentBase;
            UserImplementsDisposable = userImplementsDisposable;
            UserOverridesOnInitialized = userOverridesOnInitialized;
            UserOverridesOnInitializedAsync = userOverridesOnInitializedAsync;
            ClassFilePath = classFilePath;
            ClassSpanStart = classSpanStart;
            ClassSpanLength = classSpanLength;
        }

        public bool Equals(FieldData? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return FieldName == other.FieldName
                && ContainingClassName == other.ContainingClassName
                && ContainingClassNamespace == other.ContainingClassNamespace
                && FieldTypeDisplayString == other.FieldTypeDisplayString
                && FieldTypeOriginalDefinitionDisplayString == other.FieldTypeOriginalDefinitionDisplayString
                && TypeArgumentName == other.TypeArgumentName
                && TypeArgumentFullyQualified == other.TypeArgumentFullyQualified
                && IsStatic == other.IsStatic
                && IsGenericType == other.IsGenericType
                && TimeToLive == other.TimeToLive
                && HasInitializer == other.HasInitializer
                && FilePath == other.FilePath
                && SpanStart == other.SpanStart
                && SpanLength == other.SpanLength
                && IsPartialClass == other.IsPartialClass
                && InheritsFromComponentBase == other.InheritsFromComponentBase
                && UserImplementsDisposable == other.UserImplementsDisposable
                && UserOverridesOnInitialized == other.UserOverridesOnInitialized
                && UserOverridesOnInitializedAsync == other.UserOverridesOnInitializedAsync
                && ClassFilePath == other.ClassFilePath
                && ClassSpanStart == other.ClassSpanStart
                && ClassSpanLength == other.ClassSpanLength;
        }

        public override bool Equals(object? obj) => Equals(obj as FieldData);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (FieldName?.GetHashCode() ?? 0);
                hash = hash * 31 + (ContainingClassName?.GetHashCode() ?? 0);
                hash = hash * 31 + (ContainingClassNamespace?.GetHashCode() ?? 0);
                hash = hash * 31 + (FieldTypeDisplayString?.GetHashCode() ?? 0);
                hash = hash * 31 + (TypeArgumentName?.GetHashCode() ?? 0);
                hash = hash * 31 + IsStatic.GetHashCode();
                hash = hash * 31 + IsGenericType.GetHashCode();
                hash = hash * 31 + HasInitializer.GetHashCode();
                hash = hash * 31 + SpanStart;
                hash = hash * 31 + SpanLength;
                return hash;
            }
        }
    }
}
