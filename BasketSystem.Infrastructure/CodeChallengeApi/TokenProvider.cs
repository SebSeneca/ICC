using System.Net.Http.Json;
using BasketSystem.Application.Configuration;
using Microsoft.Extensions.Options;

namespace BasketSystem.Infrastructure.CodeChallengeApi;

public sealed class TokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<CodeChallengeApiOptions> options) : ITokenProvider, IDisposable
{
    public const string AuthClientName = "CodeChallengeAuth";

    private readonly CodeChallengeApiOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile string? _token;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_token is { } cached)
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _token ??= await LoginAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _token = null;

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(AuthClientName);

        using var response = await client.PostAsJsonAsync(
            "api/Login", new LoginRequest(_options.Email), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.Token))
            throw new InvalidOperationException("Login response did not contain a token.");

        return payload.Token;
    }

    public void Dispose() => _gate.Dispose();

    private sealed record LoginRequest(string Email);

    private sealed record LoginResponse(string? Token);
}
