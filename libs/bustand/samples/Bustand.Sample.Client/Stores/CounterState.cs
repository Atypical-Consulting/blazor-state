namespace Bustand.Sample.Client.Stores;

/// <summary>
/// State for the Counter store.
/// </summary>
/// <remarks>
/// Key concepts demonstrated:
/// <list type="bullet">
///   <item><description>C# record for immutable state (with expressions work automatically)</description></item>
///   <item><description>Simple value types in state</description></item>
///   <item><description>State is a plain data container - no behavior</description></item>
/// </list>
/// </remarks>
/// <param name="Count">The current count value.</param>
public record CounterState(int Count);
