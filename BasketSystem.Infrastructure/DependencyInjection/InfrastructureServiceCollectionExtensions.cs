using BasketSystem.Application.Configuration;
using BasketSystem.Infrastructure.CodeChallengeApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BasketSystem.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCodeChallengeApi(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CodeChallengeApiOptions>()
            .Bind(configuration.GetSection(CodeChallengeApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "CodeChallengeApi:BaseUrl is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Email), "CodeChallengeApi:Email is required.")
            .ValidateOnStart();

        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddTransient<AuthenticationDelegatingHandler>();

        services.AddHttpClient(TokenProvider.AuthClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CodeChallengeApiOptions>>().Value;
            client.BaseAddress = NormalizeBaseAddress(options.BaseUrl);
        });

        return services;
    }

    private static Uri NormalizeBaseAddress(string baseUrl)
        => new(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
}
