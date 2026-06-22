using BasketSystem.Application.Baskets;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Application.Common;
using BasketSystem.Application.Products;
using BasketSystem.Domain;

namespace BasketSystem.Application.Orders;

public sealed class OrderService(
    IBasketRepository repository,
    IBuyableProductCatalog buyableProducts,
    ICodeChallengeApiClient apiClient) : IOrderService
{
    public async Task<OrderDto> SubmitAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        var basket = repository.Find(basketId)
            ?? throw new NotFoundException($"Basket '{basketId}' was not found.");

        if (basket.Items.Count == 0)
            throw new ValidationException("Cannot submit an empty basket.");

        var lines = new List<OrderLine>();
        foreach (var item in basket.Items)
        {
            var product = await buyableProducts.FindBuyableAsync(item.ProductId, cancellationToken)
                ?? throw new BusinessRuleException(
                    $"Product '{item.ProductId}' is no longer available for purchase.");

            lines.Add(new OrderLine(product.Id, product.Name, product.Price, product.Size, item.Quantity));
        }

        var order = new Order(OrderId: null, TotalAmount: lines.Sum(l => l.TotalPrice), Lines: lines);
        var created = await apiClient.CreateOrderAsync(order, cancellationToken);

        return Map(created);
    }

    private static OrderDto Map(Order order) => new(
        order.OrderId ?? string.Empty,
        order.TotalAmount,
        order.Lines
            .Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Size, l.Quantity, l.TotalPrice))
            .ToList());
}
