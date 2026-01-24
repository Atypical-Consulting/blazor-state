namespace Bustand.Core;

/// <summary>
/// Represents an active subscription to store state changes.
/// Dispose to unsubscribe and stop receiving updates.
/// </summary>
/// <example>
/// <code>
/// var subscription = store.Subscribe(s => s.Count, () => Console.WriteLine("Count changed!"));
/// // Later, when done:
/// subscription.Dispose();
/// </code>
/// </example>
public interface ISubscription : IDisposable
{
    /// <summary>
    /// Gets whether this subscription is still active.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> after <see cref="IDisposable.Dispose"/> has been called.
    /// </remarks>
    bool IsActive { get; }
}
