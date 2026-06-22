using System.Net;
using System.Net.Http.Json;
using BasketSystem.Application.Products;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;

namespace BasketSystem.Tests.Api;

public class ProductsEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Top_returns_100_products_ordered_by_stars_then_id()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/top");
        var items = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.Should().HaveCount(100);
        items!.Select(p => (p.Stars, p.Id))
            .Should().Equal(items.OrderByDescending(p => p.Stars).ThenBy(p => p.Id).Select(p => (p.Stars, p.Id)));
        items[0].Stars.Should().Be(5);
    }

    [Fact]
    public async Task Paged_returns_page_ordered_by_price_with_metadata()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?page=1&pageSize=10");
        var paged = await response.Content.ReadFromJsonAsync<PagedProducts>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paged!.Items.Should().HaveCount(10);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(10);
        paged.TotalCount.Should().Be(250);
        paged.TotalPages.Should().Be(25);
        paged.Items.Select(p => p.Price).Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData("/api/products?page=1&pageSize=1001")]
    [InlineData("/api/products?page=1&pageSize=0")]
    [InlineData("/api/products?page=0&pageSize=10")]
    public async Task Paged_rejects_invalid_paging_with_400(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Cheapest_returns_10_products_ordered_by_price()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/cheapest");
        var items = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.Should().HaveCount(10);
        items!.Select(p => p.Price).Should().BeInAscendingOrder();
        items[0].Price.Should().Be(1m);
    }

    private sealed record PagedProducts(
        List<ProductDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
