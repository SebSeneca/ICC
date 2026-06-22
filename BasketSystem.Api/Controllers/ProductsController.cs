using BasketSystem.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace BasketSystem.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductQueryService products) : ControllerBase
{
    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetTopRanked(CancellationToken cancellationToken)
        => Ok(await products.GetTopRankedAsync(cancellationToken));

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetByPrice(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await products.GetByPriceAsync(page, pageSize, cancellationToken));

    [HttpGet("cheapest")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetCheapest(CancellationToken cancellationToken)
        => Ok(await products.GetCheapestAsync(cancellationToken));
}
