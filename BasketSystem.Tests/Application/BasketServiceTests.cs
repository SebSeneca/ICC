using BasketSystem.Application.Baskets;
using BasketSystem.Application.Common;
using BasketSystem.Application.Products;
using BasketSystem.Domain;
using BasketSystem.Infrastructure.Baskets;
using FluentAssertions;
using NSubstitute;

namespace BasketSystem.Tests.Application;

public class BasketServiceTests
{
    private static readonly Product Buyable = new(1, "Buyable", 10m, 5, 5);

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

        sut.Get(created.Id).Id.Should().Be(created.Id);
    }

    [Fact]
    public void Get_throws_not_found_for_unknown_id()
    {
        var sut = CreateSut();

        var act = () => sut.Get(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public async Task AddItem_adds_buyable_product_as_line()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        var result = await sut.AddItemAsync(basket.Id, Buyable.Id, quantity: 2);

        result.Items.Should().ContainSingle(i => i.ProductId == Buyable.Id && i.Quantity == 2);
        result.Total.Should().Be(20m);
    }

    [Fact]
    public async Task AddItem_increments_quantity_when_added_again()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        await sut.AddItemAsync(basket.Id, Buyable.Id, 2);
        var result = await sut.AddItemAsync(basket.Id, Buyable.Id, 3);

        result.Items.Should().ContainSingle(i => i.ProductId == Buyable.Id && i.Quantity == 5);
    }

    [Fact]
    public async Task AddItem_rejects_non_buyable_product()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        var act = () => sut.AddItemAsync(basket.Id, productId: 999, quantity: 1);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AddItem_rejects_quantity_below_one()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        var act = () => sut.AddItemAsync(basket.Id, Buyable.Id, quantity: 0);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddItem_throws_not_found_for_unknown_basket()
    {
        var sut = CreateSut();

        var act = () => sut.AddItemAsync(Guid.NewGuid(), Buyable.Id, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetItemQuantity_updates_existing_line()
    {
        var sut = CreateSut();
        var basket = sut.Create();
        await sut.AddItemAsync(basket.Id, Buyable.Id, 2);

        var result = sut.SetItemQuantity(basket.Id, Buyable.Id, 7);

        result.Items.Should().ContainSingle(i => i.ProductId == Buyable.Id && i.Quantity == 7);
    }

    [Fact]
    public void SetItemQuantity_rejects_quantity_below_one()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        var act = () => sut.SetItemQuantity(basket.Id, Buyable.Id, 0);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public async Task SetItemQuantity_throws_not_found_for_missing_item()
    {
        var sut = CreateSut();
        var basket = sut.Create();
        await sut.AddItemAsync(basket.Id, Buyable.Id, 1);

        var act = () => sut.SetItemQuantity(basket.Id, productId: 12345, quantity: 1);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public async Task RemoveItem_removes_existing_line()
    {
        var sut = CreateSut();
        var basket = sut.Create();
        await sut.AddItemAsync(basket.Id, Buyable.Id, 1);

        var result = sut.RemoveItem(basket.Id, Buyable.Id);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_throws_not_found_for_missing_item()
    {
        var sut = CreateSut();
        var basket = sut.Create();

        var act = () => sut.RemoveItem(basket.Id, Buyable.Id);

        act.Should().Throw<NotFoundException>();
    }

    private static BasketService CreateSut()
    {
        var buyable = Substitute.For<IBuyableProductCatalog>();
        buyable.FindBuyableAsync(Buyable.Id, Arg.Any<CancellationToken>()).Returns(Buyable);
        return new BasketService(new InMemoryBasketRepository(), buyable);
    }
}
