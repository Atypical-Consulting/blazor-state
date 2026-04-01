using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

internal sealed class ComponentModel
{
    public string Namespace { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public List<SliceFieldModel> Fields { get; set; } = null!;
    public bool UserImplementsDisposable { get; set; }
    public bool UserOverridesOnInitialized { get; set; }
    public bool UserOverridesOnInitializedAsync { get; set; }
    public Location ClassLocation { get; set; } = null!;
}

internal sealed class SliceFieldModel
{
    public string FieldName { get; set; } = null!;
    public string PropertyName { get; set; } = null!;
    public string TypeArgument { get; set; } = null!;
    public string FullTypeArgument { get; set; } = null!;
    public string? TimeToLive { get; set; }
    public string BaseKey { get; set; } = null!;
    public Location FieldLocation { get; set; } = null!;
}
