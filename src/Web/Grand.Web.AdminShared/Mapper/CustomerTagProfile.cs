using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Mapper;

public class CustomerTagProfile : Profile, IAutoMapperProfile
{
    public CustomerTagProfile()
    {
        CreateMap<CustomerTag, CustomerTagModel>();
        CreateMap<CustomerTagModel, CustomerTag>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}