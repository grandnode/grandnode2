using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Store.Models.Messages;

namespace Grand.Web.Store.Mapper;

public class ContactAttributeStoreProfile : Profile, IAutoMapperProfile
{
    public ContactAttributeStoreProfile()
    {
        CreateMap<ContactAttribute, ContactAttributeStoreModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
            .ForMember(dest => dest.ConditionAllowed, mo => mo.Ignore())
            .ForMember(dest => dest.ConditionModel, mo => mo.Ignore())
            .ForMember(dest => dest.IsReadOnly, mo => mo.Ignore());
    }

    public int Order => 0;
}
