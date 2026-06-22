using BasketSystem.Application.Baskets;
using BasketSystem.Application.Orders;
using BasketSystem.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace BasketSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IBuyableProductCatalog, BuyableProductCatalog>();
        services.AddScoped<IBasketService, BasketService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
