using System.Net;
using System.Net.Http.Json;
using BasketSystem.Application.Baskets;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Application.Orders;
using BasketSystem.Application.Products;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BasketSystem.Tests.Integration;

[Trait("Category", "Integration")]
public class CodeChallengeApiIntegrationTests
{
    [IntegrationFact]
    public async Task GetAllProducts_returns_full_catalog_with_expected_fields()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.Services.GetRequiredService<ICodeChallengeApiClient>();

        var products = await client.GetAllProductsAsync();

        products.Should().HaveCountGreaterThan(9000);
        products.Should().OnlyContain(p => p.Id > 0);
        products.Should().OnlyContain(p => p.Price > 0);
        products.Should().Contain(p => !string.IsNullOrWhiteSpace(p.Name));
    }

    [IntegrationFact]
    public async Task Top_and_cheapest_endpoints_return_expected_counts()
    {
        using var factory = new WebApplicationFactory<Program>();
        var http = factory.CreateClient();

        var top = await http.GetFromJsonAsync<List<ProductDto>>("/api/products/top");
        var cheapest = await http.GetFromJsonAsync<List<ProductDto>>("/api/products/cheapest");

        top.Should().HaveCount(100);
        cheapest.Should().HaveCount(10);
        cheapest!.Select(p => p.Price).Should().BeInAscendingOrder();
    }

    [IntegrationFact]
    public async Task Full_flow_creates_an_order_via_real_upstream()
    {
        using var factory = new WebApplicationFactory<Program>();
        var http = factory.CreateClient();

        var top = await http.GetFromJsonAsync<List<ProductDto>>("/api/products/top");
        var productId = top![0].Id;

        var basket = await (await http.PostAsync("/api/baskets", null))
            .Content.ReadFromJsonAsync<BasketDto>();
        (await http.PostAsJsonAsync($"/api/baskets/{basket!.Id}/items", new AddItemRequest(productId, 1)))
            .EnsureSuccessStatusCode();

        var response = await http.PostAsync($"/api/baskets/{basket.Id}/submit", null);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        order!.OrderId.Should().NotBeNullOrEmpty();
        order.Lines.Should().ContainSingle(l => l.ProductId == productId);
    }
}
