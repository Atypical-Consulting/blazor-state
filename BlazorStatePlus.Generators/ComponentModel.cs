using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BlazorStatePlus.Generators;

internal sealed class ComponentModel
{
    public string Namespace { get; set; }
    public string ClassName { get; set; }
    public List<SliceFieldModel> Fields { get; set; }
    public bool UserImplementsDisposable { get; set; }
    public bool UserOverridesOnInitialized { get; set; }
    public bool UserOverridesOnInitializedAsync { get; set; }
    public Location ClassLocation { get; set; }
}

internal sealed class SliceFieldModel
{
    public string FieldName { get; set; }
    public string PropertyName { get; set; }
    public string TypeArgument { get; set; }
    public string FullTypeArgument { get; set; }
    public string TimeToLive { get; set; }
    public bool AllowUpdatesOnNavigation { get; set; }
    public string BaseKey { get; set; }
    public Location FieldLocation { get; set; }
}
