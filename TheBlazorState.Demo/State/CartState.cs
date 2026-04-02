using TheBlazorState.Attributes;

namespace TheBlazorState.Demo.State;

public partial class CartState
{
    [Shared]
    public partial List<CartItem> Items { get; set; }

    [Shared]
    public partial decimal Total { get; set; }

    public CartState()
    {
        Items = [];
        Total = 0;
    }

    public void AddItem(CartItem item)
    {
        Items = [..Items, item];
        Total = Items.Sum(i => i.Price * i.Quantity);
    }
}

public record CartItem(int ProductId, string Name, decimal Price, int Quantity);
