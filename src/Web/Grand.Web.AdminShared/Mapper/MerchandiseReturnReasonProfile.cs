using Grand.Mapping;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Extensions;

namespace Grand.Web.AdminShared.Mapper;

public class MerchandiseReturnReasonProfile : Profile, IAutoMapperProfile
{
    public MerchandiseReturnReasonProfile()
    {
        CreateMap<MerchandiseReturnReason, MerchandiseReturnReasonModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.Stores, mo => mo.MapFrom(src => src.Stores.ToArray()));
        CreateMap<MerchandiseReturnReasonModel, MerchandiseReturnReason>()
            .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
            .ForMember(dest => dest.Stores, mo => mo.MapFrom(x => x.Stores != null ? x.Stores.ToList() : new List<string>()));
    }

    public int Order => 0;
}