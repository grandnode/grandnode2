using AutoMapper;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Tax;

namespace Grand.Web.AdminShared.Mapper;

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