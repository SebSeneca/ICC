using BasketSystem.Application.CodeChallenge;
using BasketSystem.Application.Common;
using BasketSystem.Application.Orders;
using BasketSystem.Application.Products;
using BasketSystem.Domain;
using BasketSystem.Infrastructure.Baskets;
using FluentAssertions;
using NSubstitute;

namespace BasketSystem.Tests.Application;

public class OrderServiceTests
{
    [Fact]
    public async Task Submit_builds_order_from_catalog_data_and_calls_upstream_once()
    {
        var repository = new InMemoryBasketRepository();
        var basket = new Basket(Guid.NewGuid());
        // Stale price/name in the basket to prove the order uses catalog data instead.
        basket.AddItem(new Product(1, "Stale name", 999m, 5, 1), 2);
        repository.Add(basket);

        var buyable = Substitute.For<IBuyableProductCatalog>();
        buyable.FindBuyableAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Product(1, "Catalog name", 10m, 5, 5));

        Order? sent = null;
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.CreateOrderAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                sent = ci.Arg<Order>();
                return Task.FromResult(sent with { OrderId = "order-99" });
            });

        var sut = new OrderService(repository, buyable, client);

        var result = await sut.SubmitAsync(basket.Id);

        result.OrderId.Should().Be("order-99");
        result.TotalAmount.Should().Be(20m);
        result.Lines.Should().ContainSingle();
        result.Lines[0].ProductName.Should().Be("Catalog name");
        result.Lines[0].UnitPrice.Should().Be(10m);
        result.Lines[0].TotalPrice.Should().Be(20m);
        sent!.TotalAmount.Should().Be(20m);
        await client.Received(1).CreateOrderAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_empty_basket_throws_validation()
    {
        var repository = new InMemoryBasketRepository();
        var basket = new Basket(Guid.NewGuid());
        repository.Add(basket);
        var sut = CreateSut(repository);

        var act = () => sut.SubmitAsync(basket.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Submit_unknown_basket_throws_not_found()
    {
        var sut = CreateSut(new InMemoryBasketRepository());

        var act = () => sut.SubmitAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_with_non_buyable_item_throws_business_rule()
    {
        var repository = new InMemoryBasketRepository();
        var basket = new Basket(Guid.NewGuid());
        basket.AddItem(new Product(1, "x", 10m, 5, 5), 1);
        repository.Add(basket);

        var buyable = Substitute.For<IBuyableProductCatalog>();
        buyable.FindBuyableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = new OrderService(repository, buyable, Substitute.For<ICodeChallengeApiClient>());

        var act = () => sut.SubmitAsync(basket.Id);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    private static OrderService CreateSut(InMemoryBasketRepository repository)
    {
        var buyable = Substitute.For<IBuyableProductCatalog>();
        var client = Substitute.For<ICodeChallengeApiClient>();
        client.CreateOrderAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Order>() with { OrderId = "order-1" }));
        return new OrderService(repository, buyable, client);
    }
}
