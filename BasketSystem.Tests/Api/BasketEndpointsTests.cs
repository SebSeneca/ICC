using System.Net;
using System.Net.Http.Json;
using BasketSystem.Application.Baskets;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;

namespace BasketSystem.Tests.Api;

public class BasketEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Create_returns_201_with_new_empty_basket()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/baskets", null);
        var basket = await response.Content.ReadFromJsonAsync<BasketDto>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        basket!.Id.Should().NotBeEmpty();
        basket.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_returns_previously_created_basket()
    {
        var client = factory.CreateClient();
        var created = await (await client.PostAsync("/api/baskets", null))
            .Content.ReadFromJsonAsync<BasketDto>();

        var response = await client.GetAsync($"/api/baskets/{created!.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<BasketDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_unknown_basket_returns_404_problem_details()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/baskets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
