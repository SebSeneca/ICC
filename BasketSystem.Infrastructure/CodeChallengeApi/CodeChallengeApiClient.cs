using System.Net.Http.Json;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Domain;

namespace BasketSystem.Infrastructure.CodeChallengeApi;

public sealed class CodeChallengeApiClient(HttpClient httpClient) : ICodeChallengeApiClient
{
    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await httpClient.GetFromJsonAsync<IReadOnlyList<ProductResponse>>(
            "api/GetAllProducts", cancellationToken);

        return products is null ? [] : products.Select(Map).ToList();
    }

    private static Product Map(ProductResponse p)
        => new(p.Id, p.Name ?? string.Empty, (decimal)p.Price, p.Size, p.Stars);

    private sealed record ProductResponse(int Id, string? Name, double Price, int Size, int Stars);
}
