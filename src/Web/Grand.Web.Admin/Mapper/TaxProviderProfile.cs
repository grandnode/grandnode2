using AutoMapper;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Mapper
{
    public class TaxProviderProfile : Profile, IAutoMapperProfile
    {
        public TaxProviderProfile()
        {
            CreateMap<ITaxProvider, TaxProviderModel>()
                .ForMember(dest => dest.FriendlyName, mo => mo.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.SystemName, mo => mo.MapFrom(src => src.SystemName))
                .ForMember(dest => dest.IsPrimaryTaxProvider, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}