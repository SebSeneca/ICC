namespace BasketSystem.Application.Products;

public sealed record ProductDto(int Id, string Name, decimal Price, int Size, int Stars);
