using AutoMapper;
using Grand.Api.DTOs.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;

namespace Grand.Api.Infrastructure.Mapper
{
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
}
