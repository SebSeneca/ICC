using System.Net;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;

namespace BasketSystem.Tests;

public class ErrorHandlingTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Unknown_route_returns_problem_details_with_404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/this-route-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
