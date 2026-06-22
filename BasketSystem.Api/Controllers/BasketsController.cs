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
}
