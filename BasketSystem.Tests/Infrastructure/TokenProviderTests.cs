using System.Net;
using System.Text;
using System.Text.Json;
using BasketSystem.Application.Configuration;
using BasketSystem.Infrastructure.CodeChallengeApi;
using BasketSystem.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasketSystem.Tests.Infrastructure;

public class TokenProviderTests
{
    [Fact]
    public async Task Caches_token_after_first_login()
    {
        var handler = new StubHttpMessageHandler(_ => TokenResponse("abc"));
        var sut = CreateSut(handler);

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        first.Should().Be("abc");
        second.Should().Be("abc");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Logs_in_again_after_invalidate()
    {
        var tokens = new Queue<string>(["t1", "t2"]);
        var handler = new StubHttpMessageHandler(_ => TokenResponse(tokens.Dequeue()));
        var sut = CreateSut(handler);

        var first = await sut.GetTokenAsync();
        sut.Invalidate();
        var second = await sut.GetTokenAsync();

        first.Should().Be("t1");
        second.Should().Be("t2");
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Sends_configured_email_to_login()
    {
        string? body = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return TokenResponse("abc");
        });
        var sut = CreateSut(handler, email: "configured@challenge.dk");

        await sut.GetTokenAsync();

        body.Should().Contain("configured@challenge.dk");
    }

    [Fact]
    public async Task Throws_when_login_response_has_no_token()
    {
        var handler = new StubHttpMessageHandler(_ => TokenResponse(token: null));
        var sut = CreateSut(handler);

        var act = () => sut.GetTokenAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static TokenProvider CreateSut(
        StubHttpMessageHandler handler, string email = "seb@challenge.dk")
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(TokenProvider.AuthClientName).Returns(
            _ => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://upstream.test/")
            });

        var options = Options.Create(new CodeChallengeApiOptions
        {
            Email = email,
            BaseUrl = "https://upstream.test"
        });

        return new TokenProvider(factory, options);
    }

    private static HttpResponseMessage TokenResponse(string? token) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new { token }), Encoding.UTF8, "application/json")
    };
}
