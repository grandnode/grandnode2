using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.RequestCustomerResolvers;

public static class RequestCustomerResolverRegistration
{
    /// <summary>
    ///     Registers the built-in request customer resolvers (scoped). Plugins add their own
    ///     <see cref="IRequestCustomerResolver" /> next to these.
    /// </summary>
    public static IServiceCollection AddRequestCustomerResolvers(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IRequestCustomerResolver, AllowAnonymousCustomerResolver>();
        serviceCollection.AddScoped<IRequestCustomerResolver, CookieCustomerResolver>();
        serviceCollection.AddScoped<IRequestCustomerResolver, GuestCookieCustomerResolver>();
        serviceCollection.AddScoped<IRequestCustomerResolver, SearchEngineCustomerResolver>();
        serviceCollection.AddScoped<IRequestCustomerResolver, ApiUserCustomerResolver>();
        return serviceCollection;
    }
}
