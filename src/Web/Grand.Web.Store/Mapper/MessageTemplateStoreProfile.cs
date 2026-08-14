using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Store.Models.Messages;

namespace Grand.Web.Store.Mapper;

public class MessageTemplateStoreProfile : Profile, IAutoMapperProfile
{
    public MessageTemplateStoreProfile()
    {
        CreateMap<MessageTemplate, MessageTemplateStoreModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AllowedTokens, mo => mo.Ignore())
            .ForMember(dest => dest.HasAttachedDownload, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableEmailAccounts, mo => mo.Ignore())
            .ForMember(dest => dest.ListOfStores, mo => mo.Ignore())
            .ForMember(dest => dest.IsReadOnly, mo => mo.Ignore());
    }

    public int Order => 0;
}
