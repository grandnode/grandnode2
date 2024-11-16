using AutoMapper;
using Grand.Module.Api.DTOs.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class CustomerGroupProfile : Profile, IAutoMapperProfile
{
    public CustomerGroupProfile()
    {
        CreateMap<CustomerGroupDto, CustomerGroup>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<CustomerGroup, CustomerGroupDto>();
    }

    public int Order => 1;
}