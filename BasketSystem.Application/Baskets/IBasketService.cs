namespace BasketSystem.Application.Baskets;

public interface IBasketService
{
    BasketDto Create();

    BasketDto Get(Guid id);

    Task<BasketDto> AddItemAsync(
        Guid basketId, int productId, int quantity, CancellationToken cancellationToken = default);

    BasketDto SetItemQuantity(Guid basketId, int productId, int quantity);

    BasketDto RemoveItem(Guid basketId, int productId);
}
