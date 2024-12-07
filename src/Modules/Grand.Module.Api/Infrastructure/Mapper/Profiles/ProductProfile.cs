﻿using AutoMapper;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class ProductProfile : Profile, IAutoMapperProfile
{
    public ProductProfile()
    {
        CreateMap<ProductDto, Product>()
            .ForMember(dest => dest.ProductCategories, mo => mo.Ignore())
            .ForMember(dest => dest.ProductCollections, mo => mo.Ignore())
            .ForMember(dest => dest.ProductPictures, mo => mo.Ignore())
            .ForMember(dest => dest.ProductSpecificationAttributes, mo => mo.Ignore())
            .ForMember(dest => dest.ProductAttributeMappings, mo => mo.Ignore())
            .ForMember(dest => dest.ProductAttributeCombinations, mo => mo.Ignore())
            .ForMember(dest => dest.TierPrices, mo => mo.Ignore())
            .ForMember(dest => dest.ProductWarehouseInventory, mo => mo.Ignore())
            .ForMember(dest => dest.CrossSellProduct, mo => mo.Ignore())
            .ForMember(dest => dest.RecommendedProduct, mo => mo.Ignore())
            .ForMember(dest => dest.RelatedProducts, mo => mo.Ignore())
            .ForMember(dest => dest.BundleProducts, mo => mo.Ignore())
            .ForMember(dest => dest.ProductTags, mo => mo.Ignore())
            .ForMember(dest => dest.LimitedToGroups, mo => mo.Ignore())
            .ForMember(dest => dest.CustomerGroups, mo => mo.Ignore())
            .ForMember(dest => dest.LimitedToStores, mo => mo.Ignore())
            .ForMember(dest => dest.Stores, mo => mo.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<Product, ProductDto>();

        CreateMap<ProductCategory, ProductCategoryDto>();
        CreateMap<ProductCollection, ProductCollectionDto>();
        CreateMap<ProductPicture, ProductPictureDto>();
        CreateMap<ProductSpecificationAttribute, ProductSpecificationAttributeDto>();
        CreateMap<TierPrice, ProductTierPriceDto>();
        CreateMap<ProductWarehouseInventory, ProductWarehouseInventoryDto>();
        CreateMap<ProductAttributeMapping, ProductAttributeMappingDto>();
        CreateMap<ProductAttributeCombination, ProductAttributeCombinationDto>();
    }

    public int Order => 1;
}