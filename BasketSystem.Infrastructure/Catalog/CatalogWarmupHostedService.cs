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

        await WarmUpAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
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
        }
    }

    private async Task WarmUpAsync(CancellationToken stoppingToken)
    {
        try
        {
            await catalog.GetProductsAsync(stoppingToken);
            logger.LogInformation("Product catalog warmed up.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial catalog warm-up failed; it will be loaded on first request.");
        }
    }
}
