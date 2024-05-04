using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Common.Extensions;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Extensions;

public static class ProductsMappingExtensions
{
    public static ProductModel ToModel(this Product entity, IDateTimeService dateTimeService)
    {
        var product = entity.MapTo<Product, ProductModel>();
        product.MarkAsNewStartDateTime = entity.MarkAsNewStartDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.MarkAsNewEndDateTime = entity.MarkAsNewEndDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.AvailableStartDateTime = entity.AvailableStartDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.AvailableEndDateTime = entity.AvailableEndDateTimeUtc.ConvertToUserTime(dateTimeService);
        product.PreOrderDateTime = entity.PreOrderDateTimeUtc.ConvertToUserTime(dateTimeService);
        return product;
    }

    public static Product ToEntity(this ProductModel model, IDateTimeService dateTimeService)
    {
        var product = model.MapTo<ProductModel, Product>();
        product.MarkAsNewStartDateTimeUtc = model.MarkAsNewStartDateTime.ConvertToUtcTime(dateTimeService);
        product.MarkAsNewEndDateTimeUtc = model.MarkAsNewEndDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableStartDateTimeUtc = model.AvailableStartDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableEndDateTimeUtc = model.AvailableEndDateTime.ConvertToUtcTime(dateTimeService);
        product.PreOrderDateTimeUtc = model.PreOrderDateTime.ConvertToUtcTime(dateTimeService);

        return product;
    }

    public static Product ToEntity(this ProductModel model, Product destination, IDateTimeService dateTimeService)
    {
        var product = model.MapTo(destination);
        product.MarkAsNewStartDateTimeUtc = model.MarkAsNewStartDateTime.ConvertToUtcTime(dateTimeService);
        product.MarkAsNewEndDateTimeUtc = model.MarkAsNewEndDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableStartDateTimeUtc = model.AvailableStartDateTime.ConvertToUtcTime(dateTimeService);
        product.AvailableEndDateTimeUtc = model.AvailableEndDateTime.ConvertToUtcTime(dateTimeService);
        product.PreOrderDateTimeUtc = model.PreOrderDateTime.ConvertToUtcTime(dateTimeService);
        return product;
    }

    public static ProductModel.ProductAttributeMappingModel ToModel(this ProductAttributeMapping entity)
    {
        return entity.MapTo<ProductAttributeMapping, ProductModel.ProductAttributeMappingModel>();
    }

    public static ProductAttributeMapping ToEntity(this ProductModel.ProductAttributeMappingModel model)
    {
        return model.MapTo<ProductModel.ProductAttributeMappingModel, ProductAttributeMapping>();
    }

    public static ProductAttributeMapping ToEntity(this ProductModel.ProductAttributeMappingModel model,
        ProductAttributeMapping destination)
    {
        return model.MapTo(destination);
    }

    public static ProductAttributeCombinationModel ToModel(this ProductAttributeCombination entity)
    {
        return entity.MapTo<ProductAttributeCombination, ProductAttributeCombinationModel>();
    }

    public static ProductModel.AddProductSpecificationAttributeModel ToModel(this ProductSpecificationAttribute entity)
    {
        return entity.MapTo<ProductSpecificationAttribute, ProductModel.AddProductSpecificationAttributeModel>();
    }

    public static ProductSpecificationAttribute ToEntity(this ProductModel.AddProductSpecificationAttributeModel model)
    {
        return model.MapTo<ProductModel.AddProductSpecificationAttributeModel, ProductSpecificationAttribute>();
    }

    public static ProductSpecificationAttribute ToEntity(this ProductModel.AddProductSpecificationAttributeModel model,
        ProductSpecificationAttribute destination)
    {
        if (model.AttributeTypeId != SpecificationAttributeType.Option)
        {
            model.SpecificationAttributeId = "";
            model.SpecificationAttributeOptionId = "";
            model.AllowFiltering = false;
        }

        return model.MapTo(destination);
    }

    public static ProductAttributeValue ToEntity(this PredefinedProductAttributeValue model)
    {
        return model.MapTo<PredefinedProductAttributeValue, ProductAttributeValue>();
    }

    public static ProductModel.TierPriceModel ToModel(this TierPrice entity, IDateTimeService dateTimeService)
    {
        var tierprice = entity.MapTo<TierPrice, ProductModel.TierPriceModel>();
        tierprice.StartDateTime = entity.StartDateTimeUtc.ConvertToUserTime(dateTimeService);
        tierprice.EndDateTime = entity.EndDateTimeUtc.ConvertToUserTime(dateTimeService);
        return tierprice;
    }

    public static TierPrice ToEntity(this ProductModel.TierPriceModel model, IDateTimeService dateTimeService)
    {
        var tierprice = model.MapTo<ProductModel.TierPriceModel, TierPrice>();
        tierprice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
        tierprice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
        return tierprice;
    }

    public static TierPrice ToEntity(this ProductModel.TierPriceModel model, TierPrice destination,
        IDateTimeService dateTimeService)
    {
        var tierprice = model.MapTo(destination);
        tierprice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
        tierprice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
        return tierprice;
    }
}