using BasketSystem.Application.Catalog;
using BasketSystem.Application.Common;
using BasketSystem.Application.Products;
using BasketSystem.Domain;
using FluentAssertions;
using NSubstitute;

namespace BasketSystem.Tests.Application;

public class ProductQueryServiceTests
{
    private static readonly IReadOnlyList<Product> Products =
    [
        new(1, "a", 30m, 1, 3),
        new(2, "b", 10m, 1, 5),
        new(3, "c", 20m, 1, 5),
        new(4, "d", 5m, 1, 1)
    ];

    [Fact]
    public async Task GetTopRanked_orders_by_stars_then_id()
    {
        var sut = CreateSut(Products);

        var result = await sut.GetTopRankedAsync();

        result.Select(p => p.Id).Should().Equal(2, 3, 1, 4);
    }

    [Fact]
    public async Task GetCheapest_orders_by_price_ascending()
    {
        var sut = CreateSut(Products);

        var result = await sut.GetCheapestAsync();

        result.Select(p => p.Id).Should().Equal(4, 2, 3, 1);
    }

    [Fact]
    public async Task GetByPrice_paginates_and_reports_metadata()
    {
        var sut = CreateSut(Products);

        var result = await sut.GetByPriceAsync(page: 1, pageSize: 2);

        result.Items.Select(p => p.Id).Should().Equal(4, 2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(4);
        result.TotalPages.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public async Task GetByPrice_rejects_invalid_paging(int page, int pageSize)
    {
        var sut = CreateSut(Products);

        var act = () => sut.GetByPriceAsync(page, pageSize);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static ProductQueryService CreateSut(IReadOnlyList<Product> products)
    {
        var catalog = Substitute.For<IProductCatalog>();
        catalog.GetProductsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(products));
        return new ProductQueryService(catalog);
    }
}
