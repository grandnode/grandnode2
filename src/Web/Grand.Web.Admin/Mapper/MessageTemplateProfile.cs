using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class MessageTemplateProfile : Profile, IAutoMapperProfile
    {
        public MessageTemplateProfile()
        {
            CreateMap<MessageTemplate, MessageTemplateModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AllowedTokens, mo => mo.Ignore())
                .ForMember(dest => dest.HasAttachedDownload, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableEmailAccounts, mo => mo.Ignore())
                .ForMember(dest => dest.ListOfStores, mo => mo.Ignore());

            CreateMap<MessageTemplateModel, MessageTemplate>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}