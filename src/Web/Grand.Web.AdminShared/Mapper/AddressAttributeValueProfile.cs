using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Extensions;

namespace Grand.Web.AdminShared.Mapper;

public class AddressAttributeValueProfile : Profile, IAutoMapperProfile
{
    public AddressAttributeValueProfile()
    {
        CreateMap<AddressAttributeValue, AddressAttributeValueModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore());
        CreateMap<AddressAttributeValueModel, AddressAttributeValue>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
    }

    public int Order => 0;
}