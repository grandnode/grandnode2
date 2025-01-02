using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.ApiExplorer;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.DTOs.Shipping;
using Grand.Module.Api.Queries.Handlers.Common;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Grand.Module.Api.Infrastructure;

public class ApiStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public void ConfigureServices(IServiceCollection services,
        IConfiguration configuration)
    {
        var apiConfig = services.BuildServiceProvider().GetService<BackendAPIConfig>();
        if (apiConfig.Enabled)
        {
            //Swagger - api description provider
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, MetadataApiDescriptionProvider>());

            //register RequestHandler
            RegisterRequestHandler(services);

            //Add JsonPatchInputFormatter
            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
            });
            services.AddScoped<ModelValidationAttribute>();
        }
    }

    public int Priority => 505;
    public bool BeforeConfigure => false;

    private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
    {
        var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }

    private void RegisterRequestHandler(IServiceCollection services)
    {
        var handlerTypes = new (Type dto, Type entity)[]
        {
            (typeof(CountryDto), typeof(Country)),
            (typeof(CurrencyDto), typeof(Currency)),
            (typeof(BrandDto), typeof(Brand)),
            (typeof(CategoryDto), typeof(Category)),
            (typeof(CollectionDto), typeof(Collection)),
            (typeof(ProductAttributeDto), typeof(ProductAttribute)),
            (typeof(ProductDto), typeof(Product)),
            (typeof(SpecificationAttributeDto), typeof(SpecificationAttribute)),
            (typeof(WarehouseDto), typeof(Warehouse)),
            (typeof(ShippingMethodDto), typeof(ShippingMethod)),
            (typeof(PickupPointDto), typeof(PickupPoint)),
            (typeof(DeliveryDateDto), typeof(DeliveryDate)),
            (typeof(VendorDto), typeof(Vendor)),
            (typeof(CustomerGroupDto), typeof(CustomerGroup)),
            (typeof(StoreDto), typeof(Store)),
            (typeof(LanguageDto), typeof(Language)),
            (typeof(PictureDto), typeof(Picture)),
            (typeof(LayoutDto), typeof(BrandLayout)),
            (typeof(LayoutDto), typeof(CollectionLayout)),
            (typeof(LayoutDto), typeof(CategoryLayout)),
            (typeof(LayoutDto), typeof(ProductLayout))
        };

        foreach (var (dto, entity) in handlerTypes)
        {
            var requestHandlerType = typeof(IRequestHandler<,>).MakeGenericType(
                typeof(GetGenericQuery<,>).MakeGenericType(dto, entity),
                typeof(IQueryable<>).MakeGenericType(dto)
            );
            var handlerImplementationType = typeof(GetGenericQueryHandler<,>).MakeGenericType(dto, entity);
            services.AddScoped(requestHandlerType, handlerImplementationType);
        }
    }

}