using BasketSystem.Domain;

namespace BasketSystem.Application.CodeChallenge;

public interface ICodeChallengeApiClient
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);

    Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
}
