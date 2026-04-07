using Microsoft.AspNetCore.Components;

namespace TheBlazorState.Demo.Components.Headless.Accordion;

public class AccordionContext
{
    private readonly HashSet<string> _openItems = [];
    private readonly Action _stateChanged;

    public bool Multiple { get; }

    public AccordionContext(bool multiple, Action stateChanged)
    {
        Multiple = multiple;
        _stateChanged = stateChanged;
    }

    public bool IsOpen(string itemId) => _openItems.Contains(itemId);

    public void Toggle(string itemId)
    {
        if (_openItems.Contains(itemId))
        {
            _openItems.Remove(itemId);
        }
        else
        {
            if (!Multiple)
                _openItems.Clear();

            _openItems.Add(itemId);
        }

        _stateChanged();
    }

    public void SetOpen(string itemId)
    {
        if (!Multiple)
            _openItems.Clear();

        _openItems.Add(itemId);
    }
}
