using BasketSystem.Application.CodeChallenge;
using BasketSystem.Domain;

namespace BasketSystem.Tests.TestSupport;

public sealed class FakeCodeChallengeApiClient : ICodeChallengeApiClient
{
    public Order? LastOrder { get; private set; }

    public Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(ProductCatalogFixture.Default);

    public Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        LastOrder = order;
        return Task.FromResult(order with { OrderId = "test-order-1" });
    }
}
