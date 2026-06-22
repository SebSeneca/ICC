using BasketSystem.Application.Common;
using BasketSystem.Application.Products;
using BasketSystem.Domain;

namespace BasketSystem.Application.Baskets;

public sealed class BasketService(IBasketRepository repository, IBuyableProductCatalog buyableProducts)
    : IBasketService
{
    public BasketDto Create()
    {
        var basket = new Basket(Guid.NewGuid());
        repository.Add(basket);
        return Map(basket);
    }

    public BasketDto Get(Guid id) => Map(GetBasketOrThrow(id));

    public async Task<BasketDto> AddItemAsync(
        Guid basketId, int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
            throw new ValidationException("quantity must be at least 1.");

        var basket = GetBasketOrThrow(basketId);

        var product = await buyableProducts.FindBuyableAsync(productId, cancellationToken)
            ?? throw new BusinessRuleException($"Product '{productId}' is not available for purchase.");

        basket.AddItem(product, quantity);
        return Map(basket);
    }

    public BasketDto SetItemQuantity(Guid basketId, int productId, int quantity)
    {
        if (quantity < 1)
            throw new ValidationException("quantity must be at least 1. Use DELETE to remove the item.");

        var basket = GetBasketOrThrow(basketId);

        if (!basket.SetItemQuantity(productId, quantity))
            throw new NotFoundException($"Product '{productId}' is not in basket '{basketId}'.");

        return Map(basket);
    }

    public BasketDto RemoveItem(Guid basketId, int productId)
    {
        var basket = GetBasketOrThrow(basketId);

        if (!basket.RemoveItem(productId))
            throw new NotFoundException($"Product '{productId}' is not in basket '{basketId}'.");

        return Map(basket);
    }

    private Basket GetBasketOrThrow(Guid id) =>
        repository.Find(id) ?? throw new NotFoundException($"Basket '{id}' was not found.");

    private static BasketDto Map(Basket basket) => new(
        basket.Id,
        basket.Items
            .Select(i => new BasketItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Size, i.Quantity, i.LineTotal))
            .ToList(),
        basket.Total);
}
