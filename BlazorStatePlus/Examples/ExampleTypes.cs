namespace BlazorStatePlus.Examples;

// Stub types used by example components to demonstrate usage patterns.
// In a real app these would live in your domain/service layer.

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
}

public class WeatherService
{
    public Task<WeatherForecast[]?> GetForecastAsync() => Task.FromResult<WeatherForecast[]?>(Array.Empty<WeatherForecast>());
}

public class ProductService
{
    public Task<ProductDetail.ProductDetailDto> GetAsync(int id)
        => Task.FromResult(new ProductDetail.ProductDetailDto(id, "Sample", "Description", 9.99m));

    public Task<bool> IsInWishlistAsync(int id) => Task.FromResult(false);
}

public class ReviewService
{
    public Task<ProductDetail.ReviewSummary> GetSummaryAsync(int id)
        => Task.FromResult(new ProductDetail.ReviewSummary(0, 0));
}
