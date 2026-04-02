using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TheBlazorState.Tests;

public class PrerenderIntegrationTests : IClassFixture<WebApplicationFactory<TheBlazorState.Demo.Program>>, IDisposable
{
    private readonly HttpClient _client;

    public PrerenderIntegrationTests(WebApplicationFactory<TheBlazorState.Demo.Program> factory)
    {
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task CounterPage_PrerenderContainsPersistentState()
    {
        var response = await _client.GetAsync("/counter");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        // In .NET 10 Blazor embeds PersistentComponentState as an HTML comment:
        // <!--Blazor-Server-Component-State:<base64>-->
        html.ShouldContain("Blazor-Server-Component-State:");
    }

    [Fact]
    public async Task WeatherPage_PrerenderContainsPersistentState()
    {
        var response = await _client.GetAsync("/weather");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        html.ShouldContain("Blazor-Server-Component-State:");
    }
}
