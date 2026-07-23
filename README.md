![Bustand banner](.github/banner.png)

# Bustand

[![NuGet](https://img.shields.io/nuget/v/Bustand.svg)](https://www.nuget.org/packages/Bustand)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)

**Zustand-inspired state management for Blazor**

Bustand brings the simplicity and elegance of [Zustand](https://github.com/pmndrs/zustand) to Blazor applications. Create stores with minimal boilerplate, use immutable state with C# records, and debug with built-in DevTools.

## Why Bustand?

- **Minimal boilerplate** - Define a store in ~10 lines of code. No actions, reducers, or dispatchers.
- **Immutable state with records** - Use C# records and `with` expressions for predictable state updates.
- **Built-in DevTools** - Time-travel debugging, state inspection, and diff viewer out of the box.
- **Works everywhere** - Supports Blazor Server, WebAssembly, and Auto rendering modes.
- **Middleware architecture** - Extend with logging, persistence, and custom middleware.
- **Selector-based subscriptions** - Components only re-render when their selected state slice changes.

## Features

- `ZustandStore<TState>` base class for creating stores
- Selector-based subscriptions with automatic change detection
- Middleware pipeline for cross-cutting concerns
- Persistence middleware for LocalStorage/SessionStorage
- DevTools page with state inspector, history, time-travel, and diff view
- Mode-aware DI (Singleton in WASM, Scoped in Server)

## Quick Start

Get from zero to a working app in under 5 minutes.

### 1. Install packages

```bash
dotnet add package Bustand
dotnet add package Bustand.DevTools
```

### 2. Create a store

```csharp
using System.Collections.Immutable;
using Bustand.Attributes;
using Bustand.Core;

// Define your state as an immutable record
public record TodoItem(Guid Id, string Text, bool IsComplete);
public record TodoState(ImmutableList<TodoItem> Items);

// Create a store by inheriting from ZustandStore<TState>
[BustandStore]
public class TodoStore : ZustandStore<TodoState>
{
    // Define the initial state
    protected override TodoState InitialState => new(ImmutableList<TodoItem>.Empty);

    // Define actions as methods that call Set()
    public void AddTodo(string text)
    {
        var item = new TodoItem(Guid.NewGuid(), text, false);
        Set(state => state with { Items = state.Items.Add(item) });
    }

    public void ToggleTodo(Guid id)
    {
        Set(state => state with
        {
            Items = state.Items
                .Select(t => t.Id == id ? t with { IsComplete = !t.IsComplete } : t)
                .ToImmutableList()
        });
    }

    public void RemoveTodo(Guid id)
    {
        Set(state => state with
        {
            Items = state.Items.Where(t => t.Id != id).ToImmutableList()
        });
    }
}
```

### 3. Register in Program.cs

```csharp
using Bustand.Extensions;
using Bustand.DevTools.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Bustand - automatically scans for [BustandStore] attributes
builder.Services.AddBustand();

// Add DevTools in development (provides state inspector, time-travel, etc.)
builder.Services.AddBustandDevTools(builder.Environment);

// ... rest of your configuration
```

### 4. Use in a component

```razor
@page "/todos"
@using Bustand.Components
@inherits ZustandComponent<TodoStore, TodoState>

<h1>Todo List</h1>

<div>
    <input @bind="newTodo" placeholder="What needs to be done?" />
    <button @onclick="AddTodo">Add</button>
</div>

<ul>
    @foreach (var item in State.Items)
    {
        <li>
            <input type="checkbox"
                   checked="@item.IsComplete"
                   @onchange="() => Store.ToggleTodo(item.Id)" />
            <span style="@(item.IsComplete ? "text-decoration: line-through" : "")">
                @item.Text
            </span>
            <button @onclick="() => Store.RemoveTodo(item.Id)">Delete</button>
        </li>
    }
</ul>

<p>@State.Items.Count(t => !t.IsComplete) items remaining</p>

@code {
    private string newTodo = "";

    private void AddTodo()
    {
        if (!string.IsNullOrWhiteSpace(newTodo))
        {
            Store.AddTodo(newTodo);
            newTodo = "";
        }
    }
}
```

That's it! Your store is now connected to your component with automatic re-rendering on state changes.

## DevTools

Bustand includes powerful DevTools for debugging your application state.

### Accessing DevTools

Navigate to `/bustand-devtools` in your application to open the DevTools page.

### Features

- **State Inspector** - View the current state of all registered stores as a collapsible JSON tree.
- **Action History** - See a timeline of all state changes with timestamps and action names.
- **Time-Travel** - Click any history entry to restore that state and see how your app looked at that point.
- **Diff View** - Compare any two states side-by-side to see exactly what changed.

> **Note:** DevTools is automatically disabled in production for security. Only enable in development.

## Persistence

Persist store state to browser storage with a simple attribute:

```csharp
using Bustand.Attributes;
using Bustand.Persistence;

[BustandStore]
[Persist(StorageType.LocalStorage)]  // or StorageType.SessionStorage
public class TodoStore : ZustandStore<TodoState>
{
    // State is automatically saved and restored
}
```

Persistence uses debouncing (300ms by default) to prevent excessive writes during rapid state changes.

## Selectors

Subscribe to specific state slices for efficient re-rendering:

```csharp
// In your component:
protected override void OnInitialized()
{
    // Only re-renders when Items.Count changes
    var count = UseState(s => s.Items.Count);
}
```

## Middleware

Extend Bustand with custom middleware:

```csharp
// Built-in logging middleware
builder.Services.AddBustand(options =>
{
    options.UseMiddleware<LoggingMiddleware<>>();
});
```

Create custom middleware by implementing `IMiddleware<TState>`:

```csharp
public class ValidationMiddleware<TState> : IMiddleware<TState> where TState : class
{
    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        // Return false to block the state change
        return IsValid(context.NewState);
    }

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        // Called after state change is applied
    }
}
```

## Advanced Topics

### Async Initialization

Load initial data from an API:

```csharp
[BustandStore]
public class UserStore : ZustandStore<UserState>
{
    private readonly IUserService _userService;

    public UserStore(IUserService userService)
    {
        _userService = userService;
    }

    protected override UserState InitialState => new(null, IsLoading: true);

    protected override async Task InitializeAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        Set(state => state with { User = user, IsLoading = false });
    }
}
```

### Background Thread Updates

Use `SetAsync` when updating state from background threads:

```csharp
public async Task RefreshDataAsync()
{
    var data = await _api.GetDataAsync();
    await SetAsync(state => state with { Data = data });
}
```

### Scoped Stores with CascadingParameter

For stores scoped to a component subtree:

```razor
@inherits ZustandComponentScoped<FormStore, FormState>

<!-- Store is passed via CascadingParameter instead of DI -->
```

## Documentation

- [Getting Started](https://github.com/phmatray/Bustand/wiki/Getting-Started)
- [Core Concepts](https://github.com/phmatray/Bustand/wiki/Core-Concepts)
- [Middleware Guide](https://github.com/phmatray/Bustand/wiki/Middleware)
- [Persistence](https://github.com/phmatray/Bustand/wiki/Persistence)
- [DevTools](https://github.com/phmatray/Bustand/wiki/DevTools)
- [API Reference](https://github.com/phmatray/Bustand/wiki/API-Reference)

## Sample Application

See the [samples](samples/) directory for a complete example application demonstrating:

- Counter (basic state management)
- TodoList (list operations)
- ShoppingCart (nested objects and complex state)

<!-- portfolio-techstack:start -->

## Tech Stack

- **.NET 10**
- Microsoft.AspNetCore.Components.WebAssembly
- Microsoft.AspNetCore.Components.WebAssembly.Server
- Bustand
- CompareNETObjects
- Scrutor
- bunit
- NSubstitute
- xunit.v3

<!-- portfolio-techstack:end -->

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Zustand](https://github.com/pmndrs/zustand) - The React state management library that inspired Bustand
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - Microsoft's framework for building interactive web UIs
