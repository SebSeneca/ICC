using BasketSystem.Application.Baskets;
using Microsoft.AspNetCore.Mvc;

namespace BasketSystem.Api.Controllers;

[ApiController]
[Route("api/baskets")]
public sealed class BasketsController(IBasketService baskets) : ControllerBase
{
    [HttpPost]
    public ActionResult<BasketDto> Create()
    {
        var basket = baskets.Create();
        return CreatedAtAction(nameof(GetById), new { id = basket.Id }, basket);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<BasketDto> GetById(Guid id) => Ok(baskets.Get(id));

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<BasketDto>> AddItem(
        Guid id, [FromBody] AddItemRequest request, CancellationToken cancellationToken)
        => Ok(await baskets.AddItemAsync(id, request.ProductId, request.Quantity, cancellationToken));

    [HttpPut("{id:guid}/items/{productId:int}")]
    public ActionResult<BasketDto> SetItemQuantity(
        Guid id, int productId, [FromBody] SetQuantityRequest request)
        => Ok(baskets.SetItemQuantity(id, productId, request.Quantity));

    [HttpDelete("{id:guid}/items/{productId:int}")]
    public ActionResult<BasketDto> RemoveItem(Guid id, int productId)
        => Ok(baskets.RemoveItem(id, productId));
}
