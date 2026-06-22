using BasketSystem.Application.Baskets;
using BasketSystem.Application.Catalog;
using BasketSystem.Application.CodeChallenge;
using BasketSystem.Application.Configuration;
using BasketSystem.Infrastructure.Baskets;
using BasketSystem.Infrastructure.Catalog;
using BasketSystem.Infrastructure.CodeChallengeApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BasketSystem.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
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

        services.AddHttpClient<ICodeChallengeApiClient, CodeChallengeApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CodeChallengeApiOptions>>().Value;
            client.BaseAddress = NormalizeBaseAddress(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
        })
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

        services.AddSingleton<IProductCatalog, CachedProductCatalog>();
        services.AddHostedService<CatalogWarmupHostedService>();

        services.AddSingleton<IBasketRepository, InMemoryBasketRepository>();

        return services;
    }

    private static Uri NormalizeBaseAddress(string baseUrl)
        => new(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
}
