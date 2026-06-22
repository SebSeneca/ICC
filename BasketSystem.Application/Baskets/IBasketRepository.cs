using BasketSystem.Domain;

namespace BasketSystem.Application.Baskets;

public interface IBasketRepository
{
    void Add(Basket basket);

    Basket? Find(Guid id);
}
