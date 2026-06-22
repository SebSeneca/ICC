using BasketSystem.Application.Catalog;
using BasketSystem.Domain;

namespace BasketSystem.Tests.TestSupport;

public sealed class TestProductCatalog(IReadOnlyList<Product> products) : IProductCatalog
{
    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(products);

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
