# Headless Blazor Components — Design Spec

## Context

The TheBlazorState.Demo project uses Tailwind CSS extensively with long, repeated class strings across pages (cards, buttons, badges, dark mode pairs). Rather than creating CSS-wrapper components (low value, leaky abstraction), we're building **headless Blazor components** — components that encapsulate **behavior** (keyboard navigation, accessibility, focus management, state) while leaving **styling entirely to the consumer** via Tailwind classes.

This fills a real gap in the Blazor ecosystem: there is no Blazor equivalent of [Headless UI](https://headlessui.com) or [Radix UI](https://www.radix-ui.com).

**Goal:** POC inside the Demo project. Validate the pattern, then extract to a standalone library if it proves useful.

## API Design

### Pattern: Compound Components (Headless UI-style)

Each feature is a **family of sub-components** that share state via `CascadingValue<TContext>`. Consumers compose them declaratively and apply their own Tailwind classes.

```razor
<Dialog @bind-Open="showSettings">
    <DialogTrigger class="btn btn-primary">Open Settings</DialogTrigger>
    <DialogOverlay class="fixed inset-0 bg-black/50" />
    <DialogContent class="card max-w-lg mx-auto mt-20 p-6">
        <DialogTitle class="text-lg font-display font-bold">Settings</DialogTitle>
        <p class="text-sm text-canvas-500">Configure below.</p>
        <DialogClose class="btn">Cancel</DialogClose>
    </DialogContent>
</Dialog>
```

### Escape Hatch: RenderFragment with Context

When the sub-component structure is too rigid, consumers can access the context directly:

```razor
<Dialog @bind-Open="showDialog">
    <ChildContent Context="ctx">
        <button @onclick="ctx.Open" class="btn">Open</button>
        @if (ctx.IsOpen)
        {
            <div class="fixed inset-0 bg-black/50" @onclick="ctx.Close" />
            <div class="card" role="dialog" aria-labelledby="@ctx.TitleId">
                <h2 id="@ctx.TitleId">Title</h2>
                <button @onclick="ctx.Close">Close</button>
            </div>
        }
    </ChildContent>
</Dialog>
```

### Code Organization Rules

- **All logic in code-behind files** (`.razor.cs`). No `@code` blocks, no `@inject` directives in `.razor` files.
- `.razor` files contain markup only (template + `CascadingValue`).
- Every component is a `partial class`.

## Shared Base Class

```csharp
// HeadlessBase.cs
public abstract class HeadlessBase : ComponentBase
{
    /// <summary>HTML element to render as (e.g., "div", "button", "a"). Null = component-specific default.</summary>
    [Parameter] public string? As { get; set; }

    /// <summary>CSS classes (Tailwind). Applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>All unmatched HTML attributes forwarded to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Renders the root element with the configured tag, class, and attributes.</summary>
    protected RenderFragment RenderRoot(RenderFragment? content = null) => builder =>
    {
        var tag = As ?? DefaultTag;
        builder.OpenElement(0, tag);
        if (!string.IsNullOrEmpty(Class))
            builder.AddAttribute(1, "class", Class);
        if (AdditionalAttributes != null)
            builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddContent(3, content ?? ChildContent);
        builder.CloseElement();
    };

    protected virtual string DefaultTag => "div";
}
```

## Components

### 1. Dialog

**Files:**
- `Components/Headless/Dialog/DialogContext.cs`
- `Components/Headless/Dialog/Dialog.razor` + `.razor.cs`
- `Components/Headless/Dialog/DialogTrigger.razor` + `.razor.cs`
- `Components/Headless/Dialog/DialogOverlay.razor` + `.razor.cs`
- `Components/Headless/Dialog/DialogContent.razor` + `.razor.cs`
- `Components/Headless/Dialog/DialogTitle.razor` + `.razor.cs`
- `Components/Headless/Dialog/DialogClose.razor` + `.razor.cs`

**Context:**
```csharp
public class DialogContext
{
    public bool IsOpen { get; }
    public string TitleId { get; }
    public Func<Task> Open { get; }
    public Func<Task> Close { get; }
    public Func<Task> Toggle { get; }
}
```

**Built-in behavior:**
- Focus trap inside `DialogContent` when open
- Escape key closes dialog
- `DialogOverlay` click closes dialog
- `aria-modal="true"`, `role="dialog"`, `aria-labelledby` (auto-linked to `DialogTitle`)
- Focus restored to trigger element on close

**Parameters:**
- `Dialog`: `Open` / `OpenChanged` (`@bind-Open`)
- `DialogTrigger`: default tag = `button`
- `DialogContent`: default tag = `div`
- `DialogTitle`: default tag = `h2`
- `DialogClose`: default tag = `button`

### 2. Dropdown (Menu)

**Files:**
- `Components/Headless/Dropdown/DropdownContext.cs`
- `Components/Headless/Dropdown/Dropdown.razor` + `.razor.cs`
- `Components/Headless/Dropdown/DropdownTrigger.razor` + `.razor.cs`
- `Components/Headless/Dropdown/DropdownPanel.razor` + `.razor.cs`
- `Components/Headless/Dropdown/DropdownItem.razor` + `.razor.cs`
- `Components/Headless/Dropdown/DropdownSeparator.razor` + `.razor.cs`

**Context:**
```csharp
public class DropdownContext
{
    public bool IsOpen { get; }
    public int FocusedIndex { get; }
    public Func<Task> Open { get; }
    public Func<Task> Close { get; }
    public Func<Task> Toggle { get; }
}
```

**Built-in behavior:**
- Arrow Up/Down to navigate items
- Enter/Space to select focused item
- Escape to close
- Home/End to jump to first/last item
- Click-outside to close
- `role="menu"`, `role="menuitem"`, `aria-expanded`
- Type-ahead search (first letter navigation)

**Parameters:**
- `Dropdown`: (no bind — always uncontrolled, opens/closes on interaction)
- `DropdownItem`: `OnClick`, `Disabled`
- `DropdownSeparator`: pure visual, default tag = `div`

### 3. Tabs

**Files:**
- `Components/Headless/Tabs/TabsContext.cs`
- `Components/Headless/Tabs/TabGroup.razor` + `.razor.cs`
- `Components/Headless/Tabs/TabList.razor` + `.razor.cs`
- `Components/Headless/Tabs/Tab.razor` + `.razor.cs`
- `Components/Headless/Tabs/TabPanels.razor` + `.razor.cs`
- `Components/Headless/Tabs/TabPanel.razor` + `.razor.cs`

**Context:**
```csharp
public class TabsContext
{
    public int SelectedIndex { get; }
    public Func<int, Task> Select { get; }
    public bool IsSelected(int index) => SelectedIndex == index;
}
```

**Built-in behavior:**
- Arrow Left/Right to navigate tabs
- Home/End to jump to first/last tab
- `role="tablist"`, `role="tab"`, `role="tabpanel"`
- `aria-selected`, `aria-controls`, `aria-labelledby`
- Automatic index tracking (tabs register themselves)

**Parameters:**
- `TabGroup`: `SelectedIndex` / `SelectedIndexChanged` (`@bind-SelectedIndex`), `DefaultIndex`
- `Tab`: `Disabled`, `ActiveClass` (convenience: merged into `Class` when selected)
- `TabPanel`: renders only when its index matches

### 4. Toggle (Switch)

**Files:**
- `Components/Headless/Toggle/Toggle.razor` + `.razor.cs`
- `Components/Headless/Toggle/ToggleContext.cs`

**Context:**
```csharp
public class ToggleContext
{
    public bool Value { get; }
    public Func<Task> Toggle { get; }
}
```

**Built-in behavior:**
- Space key to toggle
- `role="switch"`, `aria-checked`
- Renders as `<button>` by default

**Parameters:**
- `Value` / `ValueChanged` (`@bind-Value`)
- `Label` (string, sets `aria-label`)

**Usage:**
```razor
<Toggle @bind-Value="darkMode" class="relative w-11 h-6 rounded-full
        @(darkMode ? "bg-signal-500" : "bg-canvas-300")">
    <span class="block w-5 h-5 bg-white rounded-full shadow transition-transform
                 @(darkMode ? "translate-x-5" : "translate-x-0.5")" />
</Toggle>
```

> Note: Toggle is simple enough that it's a single component, not a family. The child content renders inside the button.

### 5. Accordion (Disclosure)

**Files:**
- `Components/Headless/Accordion/AccordionContext.cs`
- `Components/Headless/Accordion/Accordion.razor` + `.razor.cs`
- `Components/Headless/Accordion/AccordionItem.razor` + `.razor.cs`
- `Components/Headless/Accordion/AccordionTrigger.razor` + `.razor.cs`
- `Components/Headless/Accordion/AccordionPanel.razor` + `.razor.cs`

**Context (item-level):**
```csharp
public class AccordionItemContext
{
    public bool IsOpen { get; }
    public Func<Task> Toggle { get; }
}
```

**Built-in behavior:**
- Enter/Space to toggle item
- `aria-expanded`, `aria-controls`
- Optional: `Multiple` parameter on `Accordion` (allow multiple items open vs. single)

**Parameters:**
- `Accordion`: `Multiple` (bool, default `false`)
- `AccordionItem`: `DefaultOpen` (bool)
- `AccordionTrigger`: default tag = `button`

## Folder Structure

```
TheBlazorState.Demo/Components/Headless/
├── HeadlessBase.cs
├── Dialog/
│   ├── DialogContext.cs
│   ├── Dialog.razor
│   ├── Dialog.razor.cs
│   ├── DialogTrigger.razor
│   ├── DialogTrigger.razor.cs
│   ├── DialogOverlay.razor
│   ├── DialogOverlay.razor.cs
│   ├── DialogContent.razor
│   ├── DialogContent.razor.cs
│   ├── DialogTitle.razor
│   ├── DialogTitle.razor.cs
│   ├── DialogClose.razor
│   └── DialogClose.razor.cs
├── Dropdown/
│   ├── DropdownContext.cs
│   ├── Dropdown.razor
│   ├── Dropdown.razor.cs
│   ├── DropdownTrigger.razor
│   ├── DropdownTrigger.razor.cs
│   ├── DropdownPanel.razor
│   ├── DropdownPanel.razor.cs
│   ├── DropdownItem.razor
│   ├── DropdownItem.razor.cs
│   ├── DropdownSeparator.razor
│   └── DropdownSeparator.razor.cs
├── Tabs/
│   ├── TabsContext.cs
│   ├── TabGroup.razor
│   ├── TabGroup.razor.cs
│   ├── TabList.razor
│   ├── TabList.razor.cs
│   ├── Tab.razor
│   ├── Tab.razor.cs
│   ├── TabPanels.razor
│   ├── TabPanels.razor.cs
│   ├── TabPanel.razor
│   └── TabPanel.razor.cs
├── Toggle/
│   ├── ToggleContext.cs
│   ├── Toggle.razor
│   └── Toggle.razor.cs
└── Accordion/
    ├── AccordionContext.cs
    ├── Accordion.razor
    ├── Accordion.razor.cs
    ├── AccordionItem.razor
    ├── AccordionItem.razor.cs
    ├── AccordionTrigger.razor
    ├── AccordionTrigger.razor.cs
    ├── AccordionPanel.razor
    └── AccordionPanel.razor.cs
```

## What's Excluded (YAGNI)

- **No theming system** — consumers use Tailwind classes directly
- **No animation system** — use Tailwind transitions on your elements
- **No form components** (Input, Select, Checkbox) — out of POC scope
- **No portal/teleport rendering** — keep it simple
- **No SSR considerations** — Demo is WASM only
- **No separate NuGet package** — POC lives in Demo, extract later

## Verification Plan

1. **Build all 5 component families** in the Demo project
2. **Create a demo page** (`Components/Pages/Headless.razor`) showcasing each component with Tailwind styling
3. **Manual testing:**
   - Keyboard navigation works for each component
   - ARIA attributes render correctly (inspect with browser dev tools)
   - Dark mode styling works when consumer applies dark: classes
   - Click-outside closes Dropdown and Dialog
   - Focus trap works in Dialog
   - Tab order is correct
4. **Refactor an existing page** — replace a pattern in Settings or Board with a headless component to validate real-world usage
