using BasketSystem.Application.Catalog;
using BasketSystem.Domain;

namespace BasketSystem.Application.Products;

public sealed class BuyableProductCatalog(IProductCatalog catalog) : IBuyableProductCatalog
{
    public async Task<Product?> FindBuyableAsync(int productId, CancellationToken cancellationToken = default)
    {
        var products = await catalog.GetProductsAsync(cancellationToken);
        return ProductRanking
            .TopRanked(products, ProductQueryService.TopRankedCount)
            .FirstOrDefault(p => p.Id == productId);
    }
}
