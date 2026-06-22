namespace BasketSystem.Domain;

public sealed class BasketItem
{
    public BasketItem(int productId, string productName, decimal unitPrice, int size, int quantity)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Size = size;
        Quantity = quantity;
    }

    public int ProductId { get; }

    public string ProductName { get; }

    public decimal UnitPrice { get; }

    public int Size { get; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
