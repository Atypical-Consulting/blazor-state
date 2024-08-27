// Copyright (c) 2020-2024 Atypical Consulting SRL. All rights reserved.
// Atypical Consulting SRL licenses this file to you under the Apache 2.0 license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mutty.Generator.Models;
using Mutty.Generator.Templates;

namespace Mutty.Generator;

/// <summary>
/// A generator that creates extension methods for mutable records.
/// </summary>
[Generator]
public class MutableExtensionsGenerator : BaseSourceGenerator
{
    /// <inheritdoc />
    public override void GenerateCode(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> recordTypes)
    {
        if (recordTypes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var record in recordTypes)
        {
            var recordTokens = new RecordTokens(record);
            var recordName = recordTokens.RecordName;
            var namespaceName = recordTokens.NamespaceName;

            // Generate extension methods
            var mutableExtensionSource = new MutableExtensionsTemplate(recordTokens).GenerateCode();
            var extensionFileName = namespaceName is not null
                ? $"{namespaceName}.Extensions{recordName}.g.cs"
                : $"Extensions{recordName}.g.cs";
            AddSource(context, extensionFileName, mutableExtensionSource);
        }
    }
}
