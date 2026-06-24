using System.Collections.Concurrent;

namespace BasketSystem.Domain;

public sealed class Basket(Guid id)
{
    private readonly ConcurrentDictionary<int, BasketItem> _items = new();

    public Guid Id { get; } = id;

    public IReadOnlyCollection<BasketItem> Items => _items.Values.ToList();

    public decimal Total => _items.Values.Sum(item => item.LineTotal);

    public void AddItem(Product product, int quantity) =>
        _items.AddOrUpdate(
            product.Id,
            _ => new BasketItem(product.Id, product.Name, product.Price, product.Size, quantity),
            (_, existing) => existing.WithAdditionalQuantity(quantity));

    public bool SetItemQuantity(int productId, int quantity)
    {
        while (_items.TryGetValue(productId, out var existing))
        {
            if (_items.TryUpdate(productId, existing.WithQuantity(quantity), existing))
                return true;
        }

        return false;
    }

    public bool RemoveItem(int productId) => _items.TryRemove(productId, out _);
}
