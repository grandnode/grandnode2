using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Mapper;

public class MeasureUnitProfile : Profile, IAutoMapperProfile
{
    public MeasureUnitProfile()
    {
        CreateMap<MeasureUnit, MeasureUnitModel>();
        CreateMap<MeasureUnitModel, MeasureUnit>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}