using AutoMapper;
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
            .ForMember(dest => dest.Locales, mo => mo.Ignore());
        CreateMap<MerchandiseReturnReasonModel, MerchandiseReturnReason>()
            .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}