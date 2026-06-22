namespace BasketSystem.Domain;

public sealed class Basket(Guid id)
{
    private readonly Dictionary<int, BasketItem> _items = new();
    private readonly Lock _sync = new();

    public Guid Id { get; } = id;

    public IReadOnlyCollection<BasketItem> Items
    {
        get
        {
            lock (_sync)
                return _items.Values.ToList();
        }
    }

    public decimal Total
    {
        get
        {
            lock (_sync)
                return _items.Values.Sum(item => item.LineTotal);
        }
    }

    public void AddItem(Product product, int quantity)
    {
        lock (_sync)
        {
            if (_items.TryGetValue(product.Id, out var existing))
                existing.IncreaseQuantity(quantity);
            else
                _items[product.Id] = new BasketItem(
                    product.Id, product.Name, product.Price, product.Size, quantity);
        }
    }

    public bool SetItemQuantity(int productId, int quantity)
    {
        lock (_sync)
        {
            if (!_items.TryGetValue(productId, out var item))
                return false;

            item.SetQuantity(quantity);
            return true;
        }
    }

    public bool RemoveItem(int productId)
    {
        lock (_sync)
            return _items.Remove(productId);
    }
}
