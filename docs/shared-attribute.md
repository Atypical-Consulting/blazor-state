# The `[Shared]` Attribute

`[Shared]` marks a partial property in a state class as reactive. When the property changes, every component that injects the state class automatically re-renders. No manual subscriptions, no `StateHasChanged()` calls.

## Basic Usage

### Define a state class

```csharp
using TheBlazorState.Attributes;

public partial class CartState
{
    [Shared]
    public partial int ItemCount { get; set; }

    [Shared]
    public partial decimal Total { get; set; }
}
```

Both the class and the properties must be `partial`. The source generator implements the properties with change detection and event notification.

### Register as a scoped service

```csharp
builder.Services.AddScoped<CartState>();
```

Use **scoped** for per-circuit state (the most common case in Blazor Server). Use **singleton** only if you truly want state shared across all users.

### Use in components

```razor
@* NavBar.razor *@
@inject CartState Cart

<span class="cart-badge">@Cart.ItemCount items</span>
```

```razor
@* ProductCard.razor *@
@inject CartState Cart

<button @onclick="AddToCart">Add to Cart</button>

@code {
    private void AddToCart()
    {
        Cart.ItemCount++;
        Cart.Total += Product.Price;
    }
}
```

When `AddToCart` runs in `ProductCard`, the `NavBar` component re-renders with the updated count — automatically.

## How It Works

The source generator emits three things for each `[Shared]` property:

1. **Property implementation** with change detection:
   ```csharp
   public partial int ItemCount
   {
       get => __ItemCount_backing;
       set
       {
           if (EqualityComparer<int>.Default.Equals(__ItemCount_backing, value)) return;
           __ItemCount_backing = value;
           __ItemCount_meta.MarkDirty();
           __ItemCount_meta.RaiseChanged();
           StateChanged?.Invoke();  // Notify all subscribers
       }
   }
   ```

2. **`INotifyStateChanged` interface** on the class:
   ```csharp
   public event Action? StateChanged;
   ```

3. **Auto-subscription** in consuming components (via `InjectSubscriptionGenerator`):
   ```csharp
   // Generated in components that @inject CartState
   protected override void OnInitialized()
   {
       Cart.StateChanged += __OnSharedStateChanged;
   }
   private void __OnSharedStateChanged() => InvokeAsync(StateHasChanged);
   public void Dispose()
   {
       Cart.StateChanged -= __OnSharedStateChanged;
   }
   ```

You write the attribute. The generator writes everything else.

## StateMeta for Shared Properties

Each `[Shared]` property also gets a `Meta` companion:

```csharp
// Check if a shared property has been modified
if (Cart.ItemCountMeta.IsDirty) { /* ... */ }

// See the change history
foreach (var entry in Cart.TotalMeta.ChangeLog)
{
    Console.WriteLine($"{entry.OldValue} → {entry.NewValue}");
}
```

The Meta object tracks the same information as `[Persist]` properties (see [StateMeta](persist-attribute.md#statemeta-companion)), but without TTL or restoration metadata.

## Patterns

### State class with computed properties

Mix `[Shared]` properties with regular computed properties:

```csharp
public partial class CartState
{
    [Shared]
    public partial List<CartItem> Items { get; set; }

    // Not [Shared] — computed from Items
    public decimal Total => Items?.Sum(i => i.Price * i.Quantity) ?? 0;
    public int ItemCount => Items?.Count ?? 0;
}
```

> **Note:** Computed properties don't trigger re-renders on their own. The re-render happens when `Items` changes (because it has `[Shared]`), and the component reads the computed values during render.

### State class with methods

```csharp
public partial class AuthState
{
    [Shared]
    public partial User? CurrentUser { get; set; }

    [Shared]
    public partial bool IsAuthenticated { get; set; }

    public void Login(User user)
    {
        CurrentUser = user;
        IsAuthenticated = true;
        // Both assignments trigger StateChanged — but the component
        // only re-renders once (Blazor batches render calls)
    }

    public void Logout()
    {
        CurrentUser = null;
        IsAuthenticated = false;
    }
}
```

### Multiple state classes

Split state by domain rather than putting everything in one class:

```csharp
public partial class AuthState { /* user, tokens */ }
public partial class ThemeState { /* dark mode, density */ }
public partial class CartState  { /* items, totals */ }
public partial class NavState   { /* sidebar open, active page */ }
```

Components inject only what they need:

```razor
@inject ThemeState Theme
@inject AuthState Auth

<div class="@(Theme.IsDark ? "dark" : "light")">
    Welcome, @Auth.CurrentUser?.Name
</div>
```

## Combining [Shared] with [Persist]

`[Shared]` lives on state class properties. `[Persist]` lives on component properties. They solve different problems and can work together.

**Shared + Persisted state class:**

```csharp
public partial class ThemeState
{
    [Shared]
    public partial bool IsDark { get; set; }
}
```

**Component that persists the shared state to localStorage:**

```csharp
public partial class Settings : ComponentBase
{
    [Inject] public ThemeState Theme { get; set; } = null!;

    [Persist]
    public partial bool? SavedTheme { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        ctx.SavedTheme.Storage = StorageStrategy.LocalStorage();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && SavedThemeMeta.WasRestored && SavedTheme is not null)
        {
            Theme.IsDark = SavedTheme.Value;
        }
    }
}
```

This pattern separates concerns: `ThemeState` handles reactivity, the `Settings` component handles persistence. Other components just inject `ThemeState` and react to changes.

## Auto-Subscription Rules

The `InjectSubscriptionGenerator` automatically wires subscriptions when:

1. A component has an `[Inject]` property
2. The injected type has at least one `[Shared]` property
3. The component class is `partial`

If the component also has `[Persist]` properties, the subscription is integrated into the generated lifecycle hooks. If it only injects shared state (no `[Persist]`), the generator creates standalone `OnInitialized` / `Dispose` overrides.

### When auto-subscription doesn't apply

- Non-partial components — the generator can't extend them
- Services injecting other services — only works on `ComponentBase` subclasses
- Manual subscriptions — if you need custom logic, subscribe to `StateChanged` yourself
