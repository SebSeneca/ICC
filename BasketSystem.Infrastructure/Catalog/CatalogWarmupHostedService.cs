using BasketSystem.Application.Catalog;
using BasketSystem.Application.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BasketSystem.Infrastructure.Catalog;

public sealed class CatalogWarmupHostedService(
    IProductCatalog catalog,
    IOptions<CodeChallengeApiOptions> options,
    ILogger<CatalogWarmupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.RefreshInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await catalog.RefreshAsync(stoppingToken);
                logger.LogInformation("Product catalog refreshed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh product catalog; retrying in {Interval}.", interval);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
