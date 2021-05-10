using AutoMapper;
using Grand.Api.DTOs.Customers;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;

namespace Grand.Api.Infrastructure.Mapper
{
    public class AddressProfile : Profile, IAutoMapperProfile
    {
        public AddressProfile()
        {
            CreateMap<AddressDto, Address>()
                .ForMember(dest => dest.Attributes, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<Address, AddressDto>();
        }

        public int Order => 1;
    }
}
