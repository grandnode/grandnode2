using AutoMapper;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Common.Extensions;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Mapper;

public class ProductProfile : Profile, IAutoMapperProfile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.ProductTypeName, mo => mo.Ignore())
            .ForMember(dest => dest.AssociatedToProductId, mo => mo.Ignore())
            .ForMember(dest => dest.AssociatedToProductName, mo => mo.Ignore())
            .ForMember(dest => dest.StockQuantityStr, mo => mo.Ignore())
            .ForMember(dest => dest.CreatedOn, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOn, mo => mo.Ignore())
            .ForMember(dest => dest.PictureThumbnailUrl, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableProductLayouts, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableProductAttributes, mo => mo.Ignore())
            .ForMember(dest => dest.AddPictureModel, mo => mo.Ignore())
            .ForMember(dest => dest.ProductPictureModels, mo => mo.Ignore())
            .ForMember(dest => dest.CopyProductModel, mo => mo.Ignore())
            .ForMember(dest => dest.ProductWarehouseInventoryModels, mo => mo.Ignore())
            .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)))
            .ForMember(dest => dest.AvailableTaxCategories, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableUnits, mo => mo.Ignore())
            .ForMember(dest => dest.PrimaryStoreCurrencyCode, mo => mo.Ignore())
            .ForMember(dest => dest.BaseDimensionIn, mo => mo.Ignore())
            .ForMember(dest => dest.BaseWeightIn, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableDeliveryDates, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableWarehouses, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableBasepriceUnits, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableBasepriceBaseUnits, mo => mo.Ignore())
            .ForPath(dest => dest.CalendarModel.IncBothDate, mo => mo.MapFrom(x => x.IncBothDate));

        CreateMap<ProductModel, Product>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.VendorId, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.Coordinates, mo => mo.Ignore())
            .ForMember(dest => dest.ParentGroupedProductId, mo => mo.Ignore())
            .ForMember(dest => dest.ApprovedRatingSum, mo => mo.Ignore())
            .ForMember(dest => dest.NotApprovedRatingSum, mo => mo.Ignore())
            .ForMember(dest => dest.ApprovedTotalReviews, mo => mo.Ignore())
            .ForMember(dest => dest.NotApprovedTotalReviews, mo => mo.Ignore())
            .ForMember(dest => dest.ProductCategories, mo => mo.Ignore())
            .ForMember(dest => dest.ProductCollections, mo => mo.Ignore())
            .ForMember(dest => dest.ProductPictures, mo => mo.Ignore())
            .ForMember(dest => dest.ProductSpecificationAttributes, mo => mo.Ignore())
            .ForMember(dest => dest.ProductWarehouseInventory, mo => mo.Ignore())
            .ForMember(dest => dest.Interval, mo => mo.Ignore())
            .ForMember(dest => dest.ProductAttributeMappings, mo => mo.Ignore())
            .ForMember(dest => dest.ProductAttributeCombinations, mo => mo.Ignore())
            .ForMember(dest => dest.TierPrices, mo => mo.Ignore())
            .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore())
            .ForPath(dest => dest.IncBothDate, mo => mo.MapFrom(x => x.CalendarModel.IncBothDate));

        CreateMap<ProductAttributeMapping, ProductModel.ProductAttributeMappingModel>();

        CreateMap<ProductModel.ProductAttributeMappingModel, ProductAttributeMapping>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());

        CreateMap<ProductAttributeCombination, ProductAttributeCombinationModel>()
            .ForMember(dest => dest.UseMultipleWarehouses, mo => mo.Ignore())
            .ForMember(dest => dest.PrimaryStoreCurrencyCode, mo => mo.Ignore())
            .ForMember(dest => dest.WarehouseInventoryModels, mo => mo.Ignore());
        CreateMap<ProductAttributeCombinationModel, ProductAttributeCombination>()
            .ForMember(dest => dest.WarehouseInventory, mo => mo.Ignore())
            .ForMember(dest => dest.Id, mo => mo.Ignore());

        CreateMap<PredefinedProductAttributeValue, ProductAttributeValue>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());

        CreateMap<PredefinedProductAttributeValue, PredefinedProductAttributeValueModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.PriceAdjustmentStr, mo => mo.MapFrom(x => x.PriceAdjustment.ToString("N2")))
            .ForMember(dest => dest.WeightAdjustmentStr, mo => mo.MapFrom(x => x.WeightAdjustment.ToString("N2")));

        CreateMap<PredefinedProductAttributeValueModel, PredefinedProductAttributeValue>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));

        CreateMap<ProductSpecificationAttribute, ProductModel.AddProductSpecificationAttributeModel>()
            .ForMember(dest => dest.AvailableAttributes, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableOptions, mo => mo.Ignore());
        CreateMap<ProductModel.AddProductSpecificationAttributeModel, ProductSpecificationAttribute>();

        CreateMap<TierPrice, ProductModel.TierPriceModel>()
            .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore());

        CreateMap<ProductModel.TierPriceModel, TierPrice>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}