using BasketSystem.Domain;

namespace BasketSystem.Application.Products;

public static class ProductRanking
{
    public static IReadOnlyList<Product> TopRanked(IEnumerable<Product> products, int count)
        => products
            .OrderByDescending(p => p.Stars)
            .ThenBy(p => p.Id)
            .Take(count)
            .ToList();
}
