﻿using AutoMapper;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class CategoryProfile : Profile, IAutoMapperProfile
{
    public CategoryProfile()
    {
        CreateMap<CategoryDto, Category>()
            .ForMember(dest => dest.LimitedToGroups, mo => mo.Ignore())
            .ForMember(dest => dest.CustomerGroups, mo => mo.Ignore())
            .ForMember(dest => dest.LimitedToStores, mo => mo.Ignore())
            .ForMember(dest => dest.Stores, mo => mo.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<Category, CategoryDto>();
    }

    public int Order => 1;
}