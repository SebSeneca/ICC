using BasketSystem.Application.Catalog;
using BasketSystem.Application.Common;
using BasketSystem.Domain;

namespace BasketSystem.Application.Products;

public sealed class ProductQueryService(IProductCatalog catalog) : IProductQueryService
{
    public const int TopRankedCount = 100;
    public const int CheapestCount = 10;
    public const int MaxPageSize = 1000;

    public async Task<IReadOnlyList<ProductDto>> GetTopRankedAsync(CancellationToken cancellationToken = default)
    {
        var products = await catalog.GetProductsAsync(cancellationToken);
        return ProductRanking.TopRanked(products, TopRankedCount).Select(Map).ToList();
    }

    public async Task<PagedResult<ProductDto>> GetByPriceAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);

        var products = await catalog.GetProductsAsync(cancellationToken);
        var ordered = OrderByPrice(products);

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToList();

        return new PagedResult<ProductDto>(items, page, pageSize, products.Count);
    }

    public async Task<IReadOnlyList<ProductDto>> GetCheapestAsync(CancellationToken cancellationToken = default)
    {
        var products = await catalog.GetProductsAsync(cancellationToken);
        return OrderByPrice(products).Take(CheapestCount).Select(Map).ToList();
    }

    private static IEnumerable<Product> OrderByPrice(IEnumerable<Product> products)
        => products.OrderBy(p => p.Price).ThenBy(p => p.Id);

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0)
            throw new ValidationException("page must be greater than 0.");
        if (pageSize <= 0)
            throw new ValidationException("pageSize must be greater than 0.");
        if (pageSize > MaxPageSize)
            throw new ValidationException($"pageSize must not exceed {MaxPageSize}.");
    }

    private static ProductDto Map(Product p) => new(p.Id, p.Name, p.Price, p.Size, p.Stars);
}
