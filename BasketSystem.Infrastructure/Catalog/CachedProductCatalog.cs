using BasketSystem.Application.Catalog;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace BasketSystem.Infrastructure.Catalog;

public sealed class CachedProductCatalog(IServiceScopeFactory scopeFactory) : IProductCatalog, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile IReadOnlyList<Product>? _products;

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        if (_products is { } cached)
            return cached;

        await LoadAsync(force: false, cancellationToken);
        return _products ?? [];
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => LoadAsync(force: true, cancellationToken);

    private async Task LoadAsync(bool force, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!force && _products is not null)
                return;

            _products = await FetchAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<Product>> FetchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<ICodeChallengeApiClient>();
        return await client.GetAllProductsAsync(cancellationToken);
    }

    public void Dispose() => _gate.Dispose();
}
