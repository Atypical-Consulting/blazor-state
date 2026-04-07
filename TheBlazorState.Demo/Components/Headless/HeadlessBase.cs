using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TheBlazorState.Demo.Components.Headless;

public abstract class HeadlessBase : ComponentBase
{
    /// <summary>HTML element to render as (e.g., "div", "button", "a"). Null uses component-specific default.</summary>
    [Parameter]
    public string? As { get; set; }

    /// <summary>CSS classes (Tailwind). Applied to the root element.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>All unmatched HTML attributes forwarded to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected virtual string DefaultTag => "div";

    /// <summary>Override to add ARIA/role/event attributes to the root element.</summary>
    protected virtual void AddRootAttributes(Dictionary<string, object> attributes)
    {
    }

    protected RenderFragment RenderRoot(RenderFragment? content = null) => builder =>
    {
        var tag = As ?? DefaultTag;
        builder.OpenElement(0, tag);

        if (!string.IsNullOrEmpty(Class))
            builder.AddAttribute(1, "class", Class);

        var extraAttributes = new Dictionary<string, object>();
        AddRootAttributes(extraAttributes);

        if (AdditionalAttributes != null)
        {
            foreach (var kvp in AdditionalAttributes)
                extraAttributes[kvp.Key] = kvp.Value;
        }

        builder.AddMultipleAttributes(2, extraAttributes);
        builder.AddContent(3, content ?? ChildContent);
        builder.CloseElement();
    };
}
