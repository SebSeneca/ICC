using BasketSystem.Domain;
using BasketSystem.Infrastructure.Baskets;
using FluentAssertions;

namespace BasketSystem.Tests.Infrastructure;

public class InMemoryBasketRepositoryTests
{
    [Fact]
    public void Find_returns_added_basket()
    {
        var repository = new InMemoryBasketRepository();
        var basket = new Basket(Guid.NewGuid());
        repository.Add(basket);

        repository.Find(basket.Id).Should().BeSameAs(basket);
    }

    [Fact]
    public void Find_returns_null_for_unknown_id()
    {
        var repository = new InMemoryBasketRepository();

        repository.Find(Guid.NewGuid()).Should().BeNull();
    }
}
