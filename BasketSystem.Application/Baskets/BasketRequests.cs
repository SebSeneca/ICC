namespace BasketSystem.Application.Baskets;

public sealed record AddItemRequest(int ProductId, int Quantity);

public sealed record SetQuantityRequest(int Quantity);
