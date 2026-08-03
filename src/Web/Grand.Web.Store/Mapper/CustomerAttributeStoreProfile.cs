using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Store.Models.Customers;

namespace Grand.Web.Store.Mapper;

public class CustomerAttributeStoreProfile : Profile, IAutoMapperProfile
{
    public CustomerAttributeStoreProfile()
    {
        CreateMap<CustomerAttribute, CustomerAttributeStoreModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
            .ForMember(dest => dest.IsGlobalAttribute, mo => mo.Ignore());
    }

    public int Order => 0;
}
