using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class DropdownItem : HeadlessBase
{
    [CascadingParameter]
    private DropdownContext Context { get; set; } = null!;

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Label { get; set; }

    private int _index;

    protected override void OnInitialized()
    {
        _index = Context.RegisterItem(Label ?? ChildContent?.ToString(), Disabled);
    }

    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["id"] = Context.ItemId(_index);
        attributes["role"] = "menuitem";
        attributes["tabindex"] = "-1";

        if (Disabled)
            attributes["aria-disabled"] = "true";

        if (Context.IsFocused(_index))
            attributes["data-focused"] = "true";

        attributes["onclick"] =
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick);
    }

    private async Task HandleClick()
    {
        if (!Disabled)
        {
            await OnClick.InvokeAsync();
            await Context.Close();
        }
    }
}
