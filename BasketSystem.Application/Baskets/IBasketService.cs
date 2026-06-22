namespace BasketSystem.Application.Baskets;

public interface IBasketService
{
    BasketDto Create();

    BasketDto Get(Guid id);
}
