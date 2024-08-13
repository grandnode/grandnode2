using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class ProductsMappingExtensions
{
    public static ProductModel ToModel(this IMapper mapper, Product entity, IDateTimeService dateTimeService)
    {
        var product = mapper.Map<ProductModel>(entity);
        product.MarkAsNewStartDateTime = entity.MarkAsNewStartDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.MarkAsNewEndDateTime = entity.MarkAsNewEndDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.AvailableStartDateTime = entity.AvailableStartDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.AvailableEndDateTime = entity.AvailableEndDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.PreOrderDateTime = entity.PreOrderDateTimeUtc.ConvertToUserTime(dateTimeService);
        return product;
    }

    /*public static ProductModel ToModel(this IMapper mapper, Product entity)
    {
        var product = entity.MapTo<Product, ProductModel>();
        return product;
    }*/

    public static Product ToEntity(this IMapper mapper, ProductModel model, IDateTimeService dateTimeService)
    {
        var product = mapper.Map<Product>(model);
        product.MarkAsNewStartDateTimeUtc = model.MarkAsNewStartDateTime.ConvertToUtcTime(dateTimeService);
        product.MarkAsNewEndDateTimeUtc = model.MarkAsNewEndDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableStartDateTimeUtc = model.AvailableStartDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableEndDateTimeUtc = model.AvailableEndDateTime.ConvertToUtcTime(dateTimeService);
        product.PreOrderDateTimeUtc = model.PreOrderDateTime.ConvertToUtcTime(dateTimeService);

        return product;
    }

    public static Product ToEntity(this IMapper mapper, ProductModel model, Product destination, IDateTimeService dateTimeService)
    {
        var product = mapper.Map(model, destination);
        product.MarkAsNewStartDateTimeUtc = model.MarkAsNewStartDateTime.ConvertToUtcTime(dateTimeService);
        product.MarkAsNewEndDateTimeUtc = model.MarkAsNewEndDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableStartDateTimeUtc = model.AvailableStartDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableEndDateTimeUtc = model.AvailableEndDateTime.ConvertToUtcTime(dateTimeService);
        product.PreOrderDateTimeUtc = model.PreOrderDateTime.ConvertToUtcTime(dateTimeService);
        return product;
    }
}