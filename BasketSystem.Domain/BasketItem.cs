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

    public int Quantity { get; }

    public decimal LineTotal => UnitPrice * Quantity;

    public BasketItem WithQuantity(int quantity) =>
        new(ProductId, ProductName, UnitPrice, Size, quantity);

    public BasketItem WithAdditionalQuantity(int additional) =>
        new(ProductId, ProductName, UnitPrice, Size, Quantity + additional);
}
