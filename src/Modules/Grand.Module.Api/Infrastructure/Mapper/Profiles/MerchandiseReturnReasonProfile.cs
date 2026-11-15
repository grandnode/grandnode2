using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class MerchandiseReturnReasonProfile : Profile, IAutoMapperProfile
{
    public MerchandiseReturnReasonProfile()
    {
        CreateMap<MerchandiseReturnReasonDto, MerchandiseReturnReason>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
            .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());

        CreateMap<MerchandiseReturnReason, MerchandiseReturnReasonDto>();
    }

    public int Order => 1;
}
