using TheBlazorState.Demo.Components.Pages;

namespace TheBlazorState.Demo.Services;

public class ProductService
{
    public Task<ProductDetail.ProductDetailDto> GetAsync(int id)
        => Task.FromResult(new ProductDetail.ProductDetailDto(
            id, "Blazor State Widget", "A premium state management widget for Blazor apps.", 29.99m));

    public Task<bool> IsInWishlistAsync(int id) => Task.FromResult(false);
}

public class ReviewService
{
    public Task<ProductDetail.ReviewSummary> GetSummaryAsync(int id)
        => Task.FromResult(new ProductDetail.ReviewSummary(42, 4.3));
}
