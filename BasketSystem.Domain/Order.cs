namespace BasketSystem.Domain;

public sealed record Order(string? OrderId, decimal TotalAmount, IReadOnlyList<OrderLine> Lines);

public sealed record OrderLine(int ProductId, string ProductName, decimal UnitPrice, int Size, int Quantity)
{
    public decimal TotalPrice => UnitPrice * Quantity;
}
