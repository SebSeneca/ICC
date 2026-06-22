using BasketSystem.Application.Catalog;
using BasketSystem.Application.Configuration;
using BasketSystem.Infrastructure.Catalog;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasketSystem.Tests.Infrastructure;

public class CatalogWarmupHostedServiceTests
{
    [Fact]
    public async Task Refreshes_catalog_on_startup()
    {
        var catalog = Substitute.For<IProductCatalog>();
        var started = new TaskCompletionSource();
        catalog.RefreshAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            started.TrySetResult();
            return Task.CompletedTask;
        });
        var options = Options.Create(new CodeChallengeApiOptions { RefreshInterval = TimeSpan.FromMinutes(5) });
        var sut = new CatalogWarmupHostedService(
            catalog, options, NullLogger<CatalogWarmupHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        await catalog.Received().RefreshAsync(Arg.Any<CancellationToken>());
    }
}
