﻿using AutoMapper;
using Grand.Module.Api.DTOs.Common;
using Grand.Domain.Media;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class PictureProfile : Profile, IAutoMapperProfile
{
    public PictureProfile()
    {
        CreateMap<PictureDto, Picture>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<Picture, PictureDto>()
            .ForMember(dest => dest.PictureBinary, mo => mo.Ignore());
    }

    public int Order => 1;
}