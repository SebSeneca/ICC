namespace BasketSystem.Application.Orders;

public interface IOrderService
{
    Task<OrderDto> SubmitAsync(Guid basketId, CancellationToken cancellationToken = default);
}
