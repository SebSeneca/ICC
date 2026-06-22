using BasketSystem.Application.Products;
using BasketSystem.Domain;
using FluentAssertions;

namespace BasketSystem.Tests.Application;

public class ProductRankingTests
{
    [Fact]
    public void Orders_by_stars_desc_then_id_asc()
    {
        IReadOnlyList<Product> products =
        [
            new(1, "a", 10m, 1, 3),
            new(2, "b", 10m, 1, 5),
            new(3, "c", 10m, 1, 5),
            new(4, "d", 10m, 1, 4)
        ];

        var ranked = ProductRanking.TopRanked(products, 100);

        ranked.Select(p => p.Id).Should().Equal(2, 3, 4, 1);
    }

    [Fact]
    public void Caps_result_at_requested_count()
    {
        IReadOnlyList<Product> products = Enumerable.Range(1, 150)
            .Select(i => new Product(i, $"p{i}", 10m, 1, i % 5 + 1))
            .ToList();

        var ranked = ProductRanking.TopRanked(products, 100);

        ranked.Should().HaveCount(100);
    }

    [Fact]
    public void Returns_all_when_fewer_than_count()
    {
        IReadOnlyList<Product> products =
        [
            new(1, "a", 10m, 1, 3),
            new(2, "b", 10m, 1, 5)
        ];

        var ranked = ProductRanking.TopRanked(products, 100);

        ranked.Should().HaveCount(2);
    }
}
