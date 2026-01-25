using System.Collections.Immutable;
using Bustand.Core;
using Bustand.Persistence;

namespace Bustand.Sample.Client.Stores;

/// <summary>
/// A TodoList store demonstrating list management patterns.
/// </summary>
/// <remarks>
/// <para>
/// Key concepts demonstrated:
/// </para>
/// <list type="bullet">
///   <item><description>Working with ImmutableList (Add, Remove, Replace)</description></item>
///   <item><description>Finding and updating items in a list</description></item>
///   <item><description>Derived state (computed properties)</description></item>
///   <item><description>More complex state mutations</description></item>
/// </list>
/// <para>
/// This store is in the Client project for cross-mode compatibility.
/// Both Server and WASM/Auto pages can use this store.
/// </para>
/// </remarks>
[Persist(StorageType.Local, Key = "todos")]
public class TodoStore : ZustandStore<TodoState>
{
    /// <summary>
    /// Initialize with empty list and "All" filter.
    /// </summary>
    protected override TodoState InitialState => new(
        Items: ImmutableList<TodoItem>.Empty,
        Filter: TodoFilter.All
    );

    // ========================================
    // Derived/Computed State
    // ========================================
    // These properties compute values from state.
    // They're recalculated on each access - no caching.
    // For expensive computations, consider memoization.

    /// <summary>
    /// Get items filtered by current filter setting.
    /// This is derived state - computed from the actual state.
    /// </summary>
    public IEnumerable<TodoItem> FilteredItems => State.Filter switch
    {
        TodoFilter.Active => State.Items.Where(i => !i.IsCompleted),
        TodoFilter.Completed => State.Items.Where(i => i.IsCompleted),
        _ => State.Items
    };

    /// <summary>
    /// Count of items not yet completed.
    /// </summary>
    public int ActiveCount => State.Items.Count(i => !i.IsCompleted);

    /// <summary>
    /// Whether all items are completed.
    /// </summary>
    public bool AllCompleted => State.Items.Count > 0 && State.Items.All(i => i.IsCompleted);

    // ========================================
    // State Mutations
    // ========================================

    /// <summary>
    /// Add a new todo item.
    /// </summary>
    /// <remarks>
    /// ImmutableList.Add() returns a NEW list with the item added.
    /// The original list is unchanged - that's immutability.
    /// </remarks>
    /// <param name="text">The text for the new todo item.</param>
    public void AddTodo(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var newItem = new TodoItem(
            Id: Guid.NewGuid(),
            Text: text.Trim(),
            IsCompleted: false
        );

        Set(state => state with
        {
            Items = state.Items.Add(newItem)
        });
    }

    /// <summary>
    /// Remove a todo item by ID.
    /// </summary>
    /// <remarks>
    /// ImmutableList.RemoveAll() returns a new list without matching items.
    /// </remarks>
    /// <param name="id">The ID of the todo to remove.</param>
    public void RemoveTodo(Guid id)
    {
        Set(state => state with
        {
            Items = state.Items.RemoveAll(i => i.Id == id)
        });
    }

    /// <summary>
    /// Toggle the completed status of a todo item.
    /// </summary>
    /// <remarks>
    /// Pattern for updating an item in a list:
    /// <list type="number">
    ///   <item><description>Find the item</description></item>
    ///   <item><description>Create updated version with 'with'</description></item>
    ///   <item><description>Replace in list using ImmutableList.Replace()</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">The ID of the todo to toggle.</param>
    public void ToggleTodo(Guid id)
    {
        Set(state =>
        {
            var item = state.Items.FirstOrDefault(i => i.Id == id);
            if (item is null) return state;

            var updatedItem = item with { IsCompleted = !item.IsCompleted };
            return state with
            {
                Items = state.Items.Replace(item, updatedItem)
            };
        });
    }

    /// <summary>
    /// Update the text of a todo item.
    /// </summary>
    /// <param name="id">The ID of the todo to update.</param>
    /// <param name="newText">The new text for the todo.</param>
    public void UpdateTodoText(Guid id, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText)) return;

        Set(state =>
        {
            var item = state.Items.FirstOrDefault(i => i.Id == id);
            if (item is null) return state;

            var updatedItem = item with { Text = newText.Trim() };
            return state with
            {
                Items = state.Items.Replace(item, updatedItem)
            };
        });
    }

    /// <summary>
    /// Mark all items as completed.
    /// </summary>
    /// <remarks>
    /// ImmutableList.Select() creates a new list with transformed items.
    /// ToImmutableList() converts back to ImmutableList.
    /// </remarks>
    public void CompleteAll()
    {
        Set(state => state with
        {
            Items = state.Items
                .Select(i => i with { IsCompleted = true })
                .ToImmutableList()
        });
    }

    /// <summary>
    /// Remove all completed items.
    /// </summary>
    public void ClearCompleted()
    {
        Set(state => state with
        {
            Items = state.Items.RemoveAll(i => i.IsCompleted)
        });
    }

    /// <summary>
    /// Change the filter setting.
    /// </summary>
    /// <param name="filter">The new filter to apply.</param>
    public void SetFilter(TodoFilter filter)
    {
        Set(state => state with { Filter = filter });
    }
}
