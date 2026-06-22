using System.Net;
using System.Net.Http.Headers;

namespace BasketSystem.Infrastructure.CodeChallengeApi;

public sealed class AuthenticationDelegatingHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var retry = await CloneAsync(request);
        await AuthorizeAsync(request, refresh: false, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        await AuthorizeAsync(retry, refresh: true, cancellationToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task AuthorizeAsync(
        HttpRequestMessage request, bool refresh, CancellationToken cancellationToken)
    {
        if (refresh)
            tokenProvider.Invalidate();

        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
