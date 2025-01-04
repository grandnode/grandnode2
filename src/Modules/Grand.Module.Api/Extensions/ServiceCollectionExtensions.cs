using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.DTOs.Shipping;
using Grand.Module.Api.Queries.Handlers.Common;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grand.Module.Api.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter(this IServiceCollection services)
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
        public static void RegisterRequestHandler(this IServiceCollection services)
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
}
