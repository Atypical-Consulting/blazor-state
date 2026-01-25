using System.Collections.Immutable;
using Bustand.Core;
using Bustand.Persistence;

namespace Bustand.Sample.Client.Stores;

/// <summary>
/// A shopping cart store demonstrating advanced patterns with nested objects.
/// </summary>
/// <remarks>
/// <para>
/// Key concepts demonstrated:
/// </para>
/// <list type="bullet">
///   <item><description>Working with ImmutableDictionary</description></item>
///   <item><description>Deeply nested state updates</description></item>
///   <item><description>Async operations (SetAsync for checkout)</description></item>
///   <item><description>More complex derived state calculations</description></item>
///   <item><description>Multiple computed properties</description></item>
/// </list>
/// <para>
/// This store is in the Client project for cross-mode compatibility.
/// This is the most advanced example - study after understanding Counter and TodoList.
/// </para>
/// </remarks>
[Persist(StorageType.Session, Key = "cart")]  // Session storage - cart clears when browser closes
public class ShoppingCartStore : ZustandStore<ShoppingCartState>
{
    /// <summary>
    /// Initialize with empty cart.
    /// </summary>
    protected override ShoppingCartState InitialState => new(
        Items: ImmutableDictionary<string, CartItem>.Empty,
        IsCheckingOut: false
    );

    // ========================================
    // Derived/Computed State
    // ========================================

    /// <summary>
    /// All items in the cart as a list (for iteration).
    /// </summary>
    public IEnumerable<CartItem> Items => State.Items.Values;

    /// <summary>
    /// Total number of items (sum of quantities).
    /// </summary>
    public int TotalItemCount => State.Items.Values.Sum(i => i.Quantity);

    /// <summary>
    /// Total number of unique products.
    /// </summary>
    public int UniqueItemCount => State.Items.Count;

    /// <summary>
    /// Total price of all items.
    /// </summary>
    public decimal TotalPrice => State.Items.Values.Sum(i => i.Total);

    /// <summary>
    /// Whether the cart is empty.
    /// </summary>
    public bool IsEmpty => State.Items.Count == 0;

    // ========================================
    // State Mutations
    // ========================================

    /// <summary>
    /// Add a product to the cart or increase its quantity.
    /// </summary>
    /// <remarks>
    /// Pattern for dictionary operations:
    /// <list type="bullet">
    ///   <item><description>Use TryGetValue to check existence</description></item>
    ///   <item><description>SetItem adds or replaces a key-value pair</description></item>
    /// </list>
    /// </remarks>
    /// <param name="product">The product to add.</param>
    /// <param name="quantity">Number of units to add (default 1).</param>
    public void AddToCart(Product product, int quantity = 1)
    {
        if (quantity <= 0) return;

        Set(state =>
        {
            if (state.Items.TryGetValue(product.Id, out var existing))
            {
                // Product already in cart - increase quantity
                var updated = existing with { Quantity = existing.Quantity + quantity };
                return state with { Items = state.Items.SetItem(product.Id, updated) };
            }
            else
            {
                // New product - add to cart
                var newItem = new CartItem(product, quantity);
                return state with { Items = state.Items.Add(product.Id, newItem) };
            }
        });
    }

    /// <summary>
    /// Remove a product from the cart entirely.
    /// </summary>
    /// <param name="productId">The ID of the product to remove.</param>
    public void RemoveFromCart(string productId)
    {
        Set(state => state with
        {
            Items = state.Items.Remove(productId)
        });
    }

    /// <summary>
    /// Update the quantity of a product in the cart.
    /// Removes the item if quantity becomes 0 or less.
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="newQuantity">The new quantity (0 or less removes the item).</param>
    public void UpdateQuantity(string productId, int newQuantity)
    {
        Set(state =>
        {
            if (!state.Items.TryGetValue(productId, out var existing))
                return state;

            if (newQuantity <= 0)
            {
                // Remove item if quantity is 0 or negative
                return state with { Items = state.Items.Remove(productId) };
            }

            var updated = existing with { Quantity = newQuantity };
            return state with { Items = state.Items.SetItem(productId, updated) };
        });
    }

    /// <summary>
    /// Clear all items from the cart.
    /// </summary>
    public void ClearCart()
    {
        Set(state => state with
        {
            Items = ImmutableDictionary<string, CartItem>.Empty
        });
    }

    /// <summary>
    /// Simulate checkout process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Demonstrates SetAsync for async operations:
    /// </para>
    /// <list type="number">
    ///   <item><description>Set loading state before async work</description></item>
    ///   <item><description>Perform async operation</description></item>
    ///   <item><description>Update state after completion</description></item>
    /// </list>
    /// <para>
    /// In a real app, this would call a payment API.
    /// </para>
    /// </remarks>
    /// <returns>A task representing the checkout operation.</returns>
    public async Task CheckoutAsync()
    {
        // Set checking out flag
        Set(state => state with { IsCheckingOut = true });

        try
        {
            // Simulate API call
            await Task.Delay(1500);

            // Clear cart after successful checkout
            Set(state => state with
            {
                Items = ImmutableDictionary<string, CartItem>.Empty,
                IsCheckingOut = false
            });
        }
        catch
        {
            // Reset flag on error
            Set(state => state with { IsCheckingOut = false });
            throw;
        }
    }
}
