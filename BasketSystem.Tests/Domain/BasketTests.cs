using BasketSystem.Domain;
using FluentAssertions;

namespace BasketSystem.Tests.Domain;

public class BasketTests
{
    private static readonly Product Product = new(1, "Shirt", 12.50m, 5, 5);

    [Fact]
    public void AddItem_creates_line_and_computes_total()
    {
        var basket = new Basket(Guid.NewGuid());

        basket.AddItem(Product, 2);

        basket.Items.Should().ContainSingle();
        basket.Total.Should().Be(25m);
    }

    [Fact]
    public void AddItem_increments_existing_line()
    {
        var basket = new Basket(Guid.NewGuid());
        basket.AddItem(Product, 2);

        basket.AddItem(Product, 3);

        basket.Items.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void SetItemQuantity_returns_false_for_missing_item()
    {
        var basket = new Basket(Guid.NewGuid());

        basket.SetItemQuantity(Product.Id, 3).Should().BeFalse();
    }

    [Fact]
    public void RemoveItem_removes_line()
    {
        var basket = new Basket(Guid.NewGuid());
        basket.AddItem(Product, 1);

        basket.RemoveItem(Product.Id).Should().BeTrue();
        basket.Items.Should().BeEmpty();
    }

    [Fact]
    public void BasketItem_rejects_quantity_below_one()
    {
        var act = () => new BasketItem(1, "x", 1m, 1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
