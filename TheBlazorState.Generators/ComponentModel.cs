using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TheBlazorState.Generators;

internal sealed record ComponentModel
{
    public string Namespace { get; init; } = null!;
    public string ClassName { get; init; } = null!;
    public List<SliceFieldModel> Fields { get; init; } = null!;
    public bool UserImplementsDisposable { get; init; }
    public bool UserOverridesOnInitialized { get; init; }
    public bool UserOverridesOnInitializedAsync { get; init; }
}

internal sealed record SliceFieldModel
{
    public string FieldName { get; init; } = null!;
    public string PropertyName { get; init; } = null!;
    public string TypeArgument { get; init; } = null!;
    public string FullTypeArgument { get; init; } = null!;
    public string? TimeToLive { get; init; }
    public string BaseKey { get; init; } = null!;
    public Location FieldLocation { get; init; } = null!;
}

// --- New models for [Persist] generator ---

internal sealed record PersistComponentModel
{
    public string Namespace { get; init; } = null!;
    public string ClassName { get; init; } = null!;
    public List<PersistPropertyModel> Properties { get; init; } = null!;
    public bool UserImplementsDisposable { get; init; }
    public bool UserOverridesOnInitialized { get; init; }
    public bool UserOverridesOnInitializedAsync { get; init; }
}

internal sealed record PersistPropertyModel
{
    public string PropertyName { get; init; } = null!;
    public string FullTypeName { get; init; } = null!;
    public string? TimeToLive { get; init; }
    public string BaseKey { get; init; } = null!;
    public Location PropertyLocation { get; init; } = null!;
}
