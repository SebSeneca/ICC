using System.Collections.Concurrent;
using BasketSystem.Application.Baskets;
using BasketSystem.Domain;

namespace BasketSystem.Infrastructure.Baskets;

public sealed class InMemoryBasketRepository : IBasketRepository
{
    private readonly ConcurrentDictionary<Guid, Basket> _baskets = new();

    public void Add(Basket basket) => _baskets[basket.Id] = basket;

    public Basket? Find(Guid id) => _baskets.GetValueOrDefault(id);
}
