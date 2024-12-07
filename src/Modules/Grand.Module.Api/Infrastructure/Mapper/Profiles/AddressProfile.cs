using AutoMapper;
using Grand.Module.Api.DTOs.Customers;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class AddressProfile : Profile, IAutoMapperProfile
{
    public AddressProfile()
    {
        CreateMap<AddressDto, Address>()
            .ForMember(dest => dest.Attributes, mo => mo.Ignore());
        CreateMap<Address, AddressDto>();
    }

    public int Order => 1;
}