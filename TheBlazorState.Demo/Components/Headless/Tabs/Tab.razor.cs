using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Tabs;

public partial class Tab : HeadlessBase
{
    [CascadingParameter]
    private TabsContext Context { get; set; } = null!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? ActiveClass { get; set; }

    private int _index;

    protected override string DefaultTag => "button";

    protected override void OnInitialized()
    {
        _index = Context.RegisterTab();
    }

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        var isSelected = Context.IsSelected(_index);

        attributes["id"] = Context.TabId(_index);
        attributes["role"] = "tab";
        attributes["type"] = "button";
        attributes["aria-selected"] = isSelected.ToString().ToLowerInvariant();
        attributes["aria-controls"] = Context.PanelId(_index);
        attributes["tabindex"] = isSelected ? "0" : "-1";

        if (Disabled)
            attributes["disabled"] = true;

        attributes["onclick"] = this.OnClick(HandleClick);

        // Merge ActiveClass into Class when selected
        if (isSelected && !string.IsNullOrEmpty(ActiveClass))
        {
            var baseClass = Class ?? "";
            Class = string.IsNullOrEmpty(baseClass)
                ? ActiveClass
                : $"{baseClass} {ActiveClass}";
        }
    }

    private async Task HandleClick()
    {
        if (!Disabled)
            await Context.SelectAsync(_index);
    }
}
