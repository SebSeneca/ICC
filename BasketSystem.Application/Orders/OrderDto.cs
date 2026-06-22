namespace BasketSystem.Application.Orders;

public sealed record OrderDto(string OrderId, decimal TotalAmount, IReadOnlyList<OrderLineDto> Lines);

public sealed record OrderLineDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Size,
    int Quantity,
    decimal TotalPrice);
