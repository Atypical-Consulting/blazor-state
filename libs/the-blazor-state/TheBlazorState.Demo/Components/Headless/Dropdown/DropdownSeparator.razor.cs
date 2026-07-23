namespace TheBlazorState.Demo.Components.Headless.Dropdown;

public partial class DropdownSeparator : HeadlessBase
{
    protected override void AddRootAttributes(Dictionary<string, object> attributes)
    {
        attributes["role"] = "separator";
    }
}
