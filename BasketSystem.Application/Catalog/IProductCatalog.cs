using BasketSystem.Domain;

namespace BasketSystem.Application.Catalog;

public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
