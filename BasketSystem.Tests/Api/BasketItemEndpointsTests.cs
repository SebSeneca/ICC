using System.Net;
using System.Net.Http.Json;
using BasketSystem.Application.Baskets;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;

namespace BasketSystem.Tests.Api;

public class BasketItemEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private const int BuyableProductId = 4;     // stars 5 in the fixture
    private const int NonBuyableProductId = 1;  // stars 2 in the fixture

    [Fact]
    public async Task Add_buyable_product_then_increment()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketAsync(client);

        await AddItemAsync(client, basketId, BuyableProductId, 2);
        var basket = await AddItemAsync(client, basketId, BuyableProductId, 1);

        basket!.Items.Should().ContainSingle(i => i.ProductId == BuyableProductId && i.Quantity == 3);
    }

    [Fact]
    public async Task Add_non_buyable_product_returns_422()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/baskets/{basketId}/items", new AddItemRequest(NonBuyableProductId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Set_quantity_updates_line()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketAsync(client);
        await AddItemAsync(client, basketId, BuyableProductId, 1);

        var response = await client.PutAsJsonAsync(
            $"/api/baskets/{basketId}/items/{BuyableProductId}", new SetQuantityRequest(5));
        var basket = await response.Content.ReadFromJsonAsync<BasketDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        basket!.Items.Should().ContainSingle(i => i.ProductId == BuyableProductId && i.Quantity == 5);
    }

    [Fact]
    public async Task Set_quantity_to_zero_returns_400()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketAsync(client);
        await AddItemAsync(client, basketId, BuyableProductId, 1);

        var response = await client.PutAsJsonAsync(
            $"/api/baskets/{basketId}/items/{BuyableProductId}", new SetQuantityRequest(0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Remove_item_then_removing_again_returns_404()
    {
        var client = factory.CreateClient();
        var basketId = await CreateBasketAsync(client);
        await AddItemAsync(client, basketId, BuyableProductId, 1);

        var removed = await client.DeleteAsync($"/api/baskets/{basketId}/items/{BuyableProductId}");
        var removedAgain = await client.DeleteAsync($"/api/baskets/{basketId}/items/{BuyableProductId}");

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        removedAgain.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Add_item_to_unknown_basket_returns_404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/baskets/{Guid.NewGuid()}/items", new AddItemRequest(BuyableProductId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> CreateBasketAsync(HttpClient client)
    {
        var basket = await (await client.PostAsync("/api/baskets", null))
            .Content.ReadFromJsonAsync<BasketDto>();
        return basket!.Id;
    }

    private static async Task<BasketDto?> AddItemAsync(
        HttpClient client, Guid basketId, int productId, int quantity)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/baskets/{basketId}/items", new AddItemRequest(productId, quantity));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BasketDto>();
    }
}
