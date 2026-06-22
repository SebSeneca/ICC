using System.Net.Http.Json;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Application.Configuration;
using BasketSystem.Domain;
using Microsoft.Extensions.Options;

namespace BasketSystem.Infrastructure.CodeChallengeApi;

public sealed class CodeChallengeApiClient(HttpClient httpClient, IOptions<CodeChallengeApiOptions> options)
    : ICodeChallengeApiClient
{
    private readonly string _userEmail = options.Value.Email;

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await httpClient.GetFromJsonAsync<IReadOnlyList<ProductResponse>>(
            "api/GetAllProducts", cancellationToken);

        return products is null ? [] : products.Select(MapProduct).ToList();
    }

    public async Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var request = new CreateOrderRequest(
            _userEmail,
            order.TotalAmount,
            order.Lines
                .Select(l => new OrderLinePayload(
                    l.ProductId, l.ProductName, l.UnitPrice, l.Size.ToString(), l.Quantity, l.TotalPrice))
                .ToList());

        using var response = await httpClient.PostAsJsonAsync("api/CreateOrder", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken);
        return order with { OrderId = payload?.OrderId };
    }

    private static Product MapProduct(ProductResponse p)
        => new(p.Id, p.Name ?? string.Empty, (decimal)p.Price, p.Size, p.Stars);

    private sealed record ProductResponse(int Id, string? Name, double Price, int Size, int Stars);

    private sealed record CreateOrderRequest(
        string UserEmail, decimal TotalAmount, IReadOnlyList<OrderLinePayload> OrderLines);

    private sealed record OrderLinePayload(
        int ProductId,
        string ProductName,
        decimal ProductUnitPrice,
        string ProductSize,
        int Quantity,
        decimal TotalPrice);

    private sealed record CreateOrderResponse(string? OrderId);
}
