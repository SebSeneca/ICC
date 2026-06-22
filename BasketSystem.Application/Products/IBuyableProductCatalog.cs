using BasketSystem.Domain;

namespace BasketSystem.Application.Products;

public interface IBuyableProductCatalog
{
    Task<Product?> FindBuyableAsync(int productId, CancellationToken cancellationToken = default);
}
