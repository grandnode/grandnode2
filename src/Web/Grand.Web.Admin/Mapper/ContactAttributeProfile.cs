using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class ContactAttributeProfile : Profile, IAutoMapperProfile
    {
        public ContactAttributeProfile()
        {
            CreateMap<ContactAttribute, ContactAttributeModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.ConditionAllowed, mo => mo.Ignore())
                .ForMember(dest => dest.ConditionModel, mo => mo.Ignore());
            CreateMap<ContactAttributeModel, ContactAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.AttributeControlType, mo => mo.Ignore())
                .ForMember(dest => dest.ConditionAttribute, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.ContactAttributeValues, mo => mo.Ignore());

            CreateMap<ContactAttributeValue, ContactAttributeValueModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<ContactAttributeValueModel, ContactAttributeValue>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}