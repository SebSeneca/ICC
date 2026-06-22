using BasketSystem.Domain;

namespace BasketSystem.Application.CodeChallenge;

public interface ICodeChallengeApiClient
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
}
