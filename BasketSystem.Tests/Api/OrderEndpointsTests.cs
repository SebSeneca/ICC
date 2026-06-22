using System.Net;
using System.Net.Http.Json;
using BasketSystem.Application.Baskets;
using BasketSystem.Application.Orders;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;

namespace BasketSystem.Tests.Api;

public class OrderEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private const int BuyableProductId = 4; // stars 5, price 247 in the fixture

    [Fact]
    public async Task Submit_basket_creates_order_with_id_and_totals()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketWithItemAsync(client, BuyableProductId, quantity: 2);

        var response = await client.PostAsync($"/api/baskets/{basketId}/submit", null);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        order!.OrderId.Should().NotBeNullOrEmpty();
        order.Lines.Should().ContainSingle(l => l.ProductId == BuyableProductId && l.Quantity == 2);
        order.TotalAmount.Should().Be(order.Lines.Sum(l => l.TotalPrice));
    }

    [Fact]
    public async Task Submit_empty_basket_returns_400()
    {
        var client = factory.CreateClient();
        var basket = await (await client.PostAsync("/api/baskets", null)).Content.ReadFromJsonAsync<BasketDto>();

        var response = await client.PostAsync($"/api/baskets/{basket!.Id}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_unknown_basket_returns_404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/baskets/{Guid.NewGuid()}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> CreateBasketWithItemAsync(HttpClient client, int productId, int quantity)
    {
        var basket = await (await client.PostAsync("/api/baskets", null)).Content.ReadFromJsonAsync<BasketDto>();
        var add = await client.PostAsJsonAsync(
            $"/api/baskets/{basket!.Id}/items", new AddItemRequest(productId, quantity));
        add.EnsureSuccessStatusCode();
        return basket.Id;
    }
}
