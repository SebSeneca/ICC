namespace BasketSystem.Application.Products;

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductDto>> GetTopRankedAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ProductDto>> GetByPriceAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetCheapestAsync(CancellationToken cancellationToken = default);
}
