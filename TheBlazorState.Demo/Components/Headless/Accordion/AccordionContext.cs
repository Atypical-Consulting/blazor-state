using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public class AccordionContext(bool multiple, Action stateChanged)
{
    private readonly HashSet<string> _openItems = [];

    public bool Multiple { get; } = multiple;

    public bool IsOpen(string itemId)
    {
        return _openItems.Contains(itemId);
    }

    public void Toggle(string itemId)
    {
        if (!_openItems.Remove(itemId))
        {
            if (!Multiple)
                _openItems.Clear();

            _openItems.Add(itemId);
        }

        stateChanged();
    }

    public void SetOpen(string itemId)
    {
        if (!Multiple)
            _openItems.Clear();

        _openItems.Add(itemId);
    }
}
