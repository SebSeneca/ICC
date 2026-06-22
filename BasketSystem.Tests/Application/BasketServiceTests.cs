using BasketSystem.Application.Baskets;
using BasketSystem.Application.Common;
using BasketSystem.Infrastructure.Baskets;
using FluentAssertions;

namespace BasketSystem.Tests.Application;

public class BasketServiceTests
{
    [Fact]
    public void Create_returns_basket_with_new_id_and_no_items()
    {
        var sut = CreateSut();

        var basket = sut.Create();

        basket.Id.Should().NotBeEmpty();
        basket.Items.Should().BeEmpty();
        basket.Total.Should().Be(0m);
    }

    [Fact]
    public void Get_returns_previously_created_basket()
    {
        var sut = CreateSut();
        var created = sut.Create();

        var fetched = sut.Get(created.Id);

        fetched.Id.Should().Be(created.Id);
    }

    [Fact]
    public void Get_throws_not_found_for_unknown_id()
    {
        var sut = CreateSut();

        var act = () => sut.Get(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    private static BasketService CreateSut() => new(new InMemoryBasketRepository());
}
