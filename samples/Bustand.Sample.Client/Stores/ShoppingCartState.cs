using System.Collections.Immutable;

namespace Bustand.Sample.Client.Stores;

/// <summary>
/// A product that can be added to the cart.
/// In a real app, this might come from an API.
/// </summary>
/// <param name="Id">Unique product identifier.</param>
/// <param name="Name">Product display name.</param>
/// <param name="Price">Price per unit.</param>
/// <param name="Category">Product category for grouping/filtering.</param>
public record Product(string Id, string Name, decimal Price, string Category);

/// <summary>
/// An item in the shopping cart (product + quantity).
/// </summary>
/// <remarks>
/// Key concepts demonstrated:
/// <list type="bullet">
///   <item><description>Nested record containing another record (Product)</description></item>
///   <item><description>Computed property (Total) derived from other properties</description></item>
/// </list>
/// </remarks>
/// <param name="Product">The product in this cart item.</param>
/// <param name="Quantity">Number of units in cart.</param>
public record CartItem(Product Product, int Quantity)
{
    /// <summary>
    /// Computed total for this line item.
    /// </summary>
    public decimal Total => Product.Price * Quantity;
}

/// <summary>
/// State for the ShoppingCart store.
/// </summary>
/// <remarks>
/// Key concepts demonstrated:
/// <list type="bullet">
///   <item><description>Deeply nested state (State > CartItem > Product)</description></item>
///   <item><description>ImmutableDictionary for keyed collections</description></item>
///   <item><description>Multiple computed properties</description></item>
/// </list>
/// </remarks>
/// <param name="Items">Cart items keyed by product ID for fast lookup. Using ImmutableDictionary instead of ImmutableList when you need key-based access.</param>
/// <param name="IsCheckingOut">Whether checkout is in progress.</param>
public record ShoppingCartState(
    ImmutableDictionary<string, CartItem> Items,
    bool IsCheckingOut
);
