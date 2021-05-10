using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Mapper
{
    public class MeasureWeightProfile : Profile, IAutoMapperProfile
    {
        public MeasureWeightProfile()
        {
            CreateMap<MeasureWeight, MeasureWeightModel>()
                .ForMember(dest => dest.IsPrimaryWeight, mo => mo.Ignore());

            CreateMap<MeasureWeightModel, MeasureWeight>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}