using System.Net;
using BasketSystem.Infrastructure.CodeChallengeApi;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;
using NSubstitute;

namespace BasketSystem.Tests.Infrastructure;

public class AuthenticationDelegatingHandlerTests
{
    [Fact]
    public async Task Attaches_bearer_token_from_provider()
    {
        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.GetTokenAsync(Arg.Any<CancellationToken>()).Returns("token-1");
        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = BuildClient(tokenProvider, inner);

        await client.GetAsync("/products");

        inner.Requests[0].Headers.Authorization!.ToString().Should().Be("Bearer token-1");
    }

    [Fact]
    public async Task Refreshes_token_and_retries_once_on_unauthorized()
    {
        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.GetTokenAsync(Arg.Any<CancellationToken>()).Returns("stale", "fresh");
        var responses = new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK)
        ]);
        var inner = new StubHttpMessageHandler(_ => responses.Dequeue());
        var client = BuildClient(tokenProvider, inner);

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.CallCount.Should().Be(2);
        tokenProvider.Received(1).Invalidate();
        inner.Requests[1].Headers.Authorization!.ToString().Should().Be("Bearer fresh");
    }

    [Fact]
    public async Task Does_not_retry_when_first_attempt_succeeds()
    {
        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.GetTokenAsync(Arg.Any<CancellationToken>()).Returns("token-1");
        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = BuildClient(tokenProvider, inner);

        await client.GetAsync("/products");

        inner.CallCount.Should().Be(1);
        tokenProvider.DidNotReceive().Invalidate();
    }

    private static HttpClient BuildClient(ITokenProvider tokenProvider, HttpMessageHandler inner)
    {
        var handler = new AuthenticationDelegatingHandler(tokenProvider) { InnerHandler = inner };
        return new HttpClient(handler) { BaseAddress = new Uri("https://upstream.test/") };
    }
}
