using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Mapper
{
    public class StateProvinceProfile : Profile, IAutoMapperProfile
    {
        public StateProvinceProfile()
        {
            CreateMap<StateProvince, StateProvinceModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<StateProvinceModel, StateProvince>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}