using System.Collections.Immutable;

namespace Bustand.Sample.Client.Stores;

/// <summary>
/// A single todo item.
/// </summary>
/// <remarks>
/// Key concepts demonstrated:
/// <list type="bullet">
///   <item><description>Nested records in state</description></item>
///   <item><description>Using Guid for unique identification</description></item>
/// </list>
/// </remarks>
/// <param name="Id">Unique identifier for the todo item.</param>
/// <param name="Text">The todo item text.</param>
/// <param name="IsCompleted">Whether the item has been completed.</param>
public record TodoItem(Guid Id, string Text, bool IsCompleted);

/// <summary>
/// State for the TodoList store.
/// </summary>
/// <remarks>
/// Key concepts demonstrated:
/// <list type="bullet">
///   <item><description>Using ImmutableList for collections (from System.Collections.Immutable)</description></item>
///   <item><description>Collections in state should always be immutable</description></item>
///   <item><description>Filter property shows derived/computed state</description></item>
/// </list>
/// </remarks>
/// <param name="Items">All todo items.</param>
/// <param name="Filter">Current filter (All, Active, Completed).</param>
public record TodoState(ImmutableList<TodoItem> Items, TodoFilter Filter);

/// <summary>
/// Filter options for displaying todos.
/// </summary>
public enum TodoFilter
{
    /// <summary>Show all todos.</summary>
    All,
    /// <summary>Show only active (not completed) todos.</summary>
    Active,
    /// <summary>Show only completed todos.</summary>
    Completed
}
