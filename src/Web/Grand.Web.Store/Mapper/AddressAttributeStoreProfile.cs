using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Store.Models.Common;

namespace Grand.Web.Store.Mapper;

public class AddressAttributeStoreProfile : Profile, IAutoMapperProfile
{
    public AddressAttributeStoreProfile()
    {
        CreateMap<AddressAttribute, AddressAttributeStoreModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
            .ForMember(dest => dest.IsGlobalAttribute, mo => mo.Ignore());
    }

    public int Order => 0;
}
