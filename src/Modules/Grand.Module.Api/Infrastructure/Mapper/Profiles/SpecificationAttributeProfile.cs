﻿using AutoMapper;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class SpecificationAttributeProfile : Profile, IAutoMapperProfile
{
    public SpecificationAttributeProfile()
    {
        CreateMap<SpecificationAttributeDto, SpecificationAttribute>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<SpecificationAttribute, SpecificationAttributeDto>();

        CreateMap<SpecificationAttributeOption, SpecificationAttributeOptionDto>();

        CreateMap<SpecificationAttributeOptionDto, SpecificationAttributeOption>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore());
    }

    public int Order => 1;
}