using BasketSystem.Application.Common;
using BasketSystem.Domain;

namespace BasketSystem.Application.Baskets;

public sealed class BasketService(IBasketRepository repository) : IBasketService
{
    public BasketDto Create()
    {
        var basket = new Basket(Guid.NewGuid());
        repository.Add(basket);
        return Map(basket);
    }

    public BasketDto Get(Guid id)
    {
        var basket = repository.Find(id)
            ?? throw new NotFoundException($"Basket '{id}' was not found.");

        return Map(basket);
    }

    private static BasketDto Map(Basket basket) => new(
        basket.Id,
        basket.Items
            .Select(i => new BasketItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Size, i.Quantity, i.LineTotal))
            .ToList(),
        basket.Total);
}
