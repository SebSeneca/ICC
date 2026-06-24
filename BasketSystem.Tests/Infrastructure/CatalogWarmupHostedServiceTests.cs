using BasketSystem.Application.Catalog;
using BasketSystem.Application.Configuration;
using BasketSystem.Domain;
using BasketSystem.Infrastructure.Catalog;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasketSystem.Tests.Infrastructure;

public class CatalogWarmupHostedServiceTests
{
    [Fact]
    public async Task Warms_up_catalog_on_startup_via_shared_load()
    {
        var catalog = Substitute.For<IProductCatalog>();
        var warmed = new TaskCompletionSource();
        catalog.GetProductsAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            warmed.TrySetResult();
            return Task.FromResult<IReadOnlyList<Product>>([]);
        });
        var options = Options.Create(new CodeChallengeApiOptions { RefreshInterval = TimeSpan.FromMinutes(5) });
        var sut = new CatalogWarmupHostedService(
            catalog, options, NullLogger<CatalogWarmupHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await warmed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        await catalog.Received().GetProductsAsync(Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
    }
}
