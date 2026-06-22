using BasketSystem.Application.CodeChallenge;
using BasketSystem.Domain;
using BasketSystem.Infrastructure.Catalog;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BasketSystem.Tests.Infrastructure;

public class CachedProductCatalogTests
{
    private static readonly IReadOnlyList<Product> CatalogV1 = [new Product(1, "A", 10m, 1, 5)];
    private static readonly IReadOnlyList<Product> CatalogV2 = [new Product(2, "B", 20m, 2, 4)];

    [Fact]
    public async Task Loads_once_and_serves_from_cache()
    {
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.GetAllProductsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(CatalogV1));
        var sut = CreateSut(client);

        var first = await sut.GetProductsAsync();
        var second = await sut.GetProductsAsync();

        first.Should().BeEquivalentTo(CatalogV1);
        second.Should().BeEquivalentTo(CatalogV1);
        await client.Received(1).GetAllProductsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Concurrent_cold_requests_fetch_once()
    {
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.GetAllProductsAsync(Arg.Any<CancellationToken>()).Returns(_ => Delayed(CatalogV1));
        var sut = CreateSut(client);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => sut.GetProductsAsync()));

        results.Should().OnlyContain(r => r.Count == 1);
        await client.Received(1).GetAllProductsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_replaces_catalog()
    {
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CatalogV1), Task.FromResult(CatalogV2));
        var sut = CreateSut(client);

        await sut.GetProductsAsync();
        await sut.RefreshAsync();
        var afterRefresh = await sut.GetProductsAsync();

        afterRefresh.Should().BeEquivalentTo(CatalogV2);
        await client.Received(2).GetAllProductsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Keeps_previous_catalog_when_refresh_fails()
    {
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CatalogV1), _ => throw new HttpRequestException("upstream down"));
        var sut = CreateSut(client);

        await sut.GetProductsAsync();
        var refresh = () => sut.RefreshAsync();

        await refresh.Should().ThrowAsync<HttpRequestException>();
        (await sut.GetProductsAsync()).Should().BeEquivalentTo(CatalogV1);
    }

    private static async Task<IReadOnlyList<Product>> Delayed(IReadOnlyList<Product> products)
    {
        await Task.Delay(50);
        return products;
    }

    private static CachedProductCatalog CreateSut(ICodeChallengeApiClient client)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        var provider = services.BuildServiceProvider();
        return new CachedProductCatalog(provider.GetRequiredService<IServiceScopeFactory>());
    }
}
