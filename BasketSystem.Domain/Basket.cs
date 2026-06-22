namespace BasketSystem.Domain;

public sealed class Basket(Guid id)
{
    private readonly Dictionary<int, BasketItem> _items = new();

    public Guid Id { get; } = id;

    public IReadOnlyCollection<BasketItem> Items => _items.Values;

    public decimal Total => _items.Values.Sum(item => item.LineTotal);
}
