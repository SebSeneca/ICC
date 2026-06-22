using BasketSystem.Domain;

namespace BasketSystem.Tests.TestSupport;

public static class ProductCatalogFixture
{
    // 250 products: price descends with id (so cheapest are the highest ids),
    // stars cycle 1..5 (50 products per star value).
    public static readonly IReadOnlyList<Product> Default = Enumerable.Range(1, 250)
        .Select(i => new Product(Id: i, Name: $"Product {i}", Price: 251 - i, Size: i % 50, Stars: (i % 5) + 1))
        .ToList();
}
