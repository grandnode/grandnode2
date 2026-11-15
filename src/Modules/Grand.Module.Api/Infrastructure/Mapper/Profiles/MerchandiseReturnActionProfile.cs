using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class MerchandiseReturnActionProfile : Profile, IAutoMapperProfile
{
    public MerchandiseReturnActionProfile()
    {
        CreateMap<MerchandiseReturnActionDto, MerchandiseReturnAction>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());

        CreateMap<MerchandiseReturnAction, MerchandiseReturnActionDto>();
    }

    public int Order => 1;
}
