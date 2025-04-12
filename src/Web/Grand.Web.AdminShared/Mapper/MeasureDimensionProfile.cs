using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Mapper;

public class MeasureDimensionProfile : Profile, IAutoMapperProfile
{
    public MeasureDimensionProfile()
    {
        CreateMap<MeasureDimension, MeasureDimensionModel>()
            .ForMember(dest => dest.IsPrimaryDimension, mo => mo.Ignore());

        CreateMap<MeasureDimensionModel, MeasureDimension>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}