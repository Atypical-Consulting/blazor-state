namespace Bustand.DevTools.Models;

/// <summary>
/// Immutable snapshot of a store's state at a specific point in time.
/// Used for state history tracking and time-travel debugging.
/// </summary>
/// <param name="Index">The position of this snapshot in the history (0-based).</param>
/// <param name="State">The actual state object at this point in time. Used for time-travel restoration.</param>
/// <param name="ActionName">The name of the action that triggered this state change, or null if not specified.</param>
/// <param name="Timestamp">The timestamp when this state change occurred.</param>
/// <param name="StateJson">Pre-serialized JSON representation of the state for display purposes.</param>
/// <remarks>
/// <para>
/// <b>Design notes:</b>
/// </para>
/// <list type="bullet">
/// <item><see cref="Index"/> provides stable ordering for history navigation and time-travel.</item>
/// <item><see cref="State"/> preserves the original object reference for accurate state restoration.</item>
/// <item><see cref="StateJson"/> is pre-serialized to avoid repeated serialization when rendering the UI.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var snapshot = new StateSnapshot(
///     Index: 0,
///     State: myState,
///     ActionName: "Increment",
///     Timestamp: DateTimeOffset.UtcNow,
///     StateJson: "{\"count\": 1}"
/// );
/// </code>
/// </example>
public sealed record StateSnapshot(
    int Index,
    object State,
    string? ActionName,
    DateTimeOffset Timestamp,
    string StateJson
);
