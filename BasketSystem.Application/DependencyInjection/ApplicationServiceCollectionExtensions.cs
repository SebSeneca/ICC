using BasketSystem.Application.Baskets;
using BasketSystem.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace BasketSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IBasketService, BasketService>();
        return services;
    }
}
