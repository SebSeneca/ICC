namespace BasketSystem.Application.Baskets;

public sealed record BasketDto(Guid Id, IReadOnlyList<BasketItemDto> Items, decimal Total);

public sealed record BasketItemDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Size,
    int Quantity,
    decimal LineTotal);
