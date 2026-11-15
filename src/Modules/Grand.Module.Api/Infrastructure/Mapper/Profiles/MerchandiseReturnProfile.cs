using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class MerchandiseReturnProfile : Profile, IAutoMapperProfile
{
    public MerchandiseReturnProfile()
    {
        CreateMap<MerchandiseReturn, MerchandiseReturnDto>()
            .ForMember(dest => dest.NextStatuses, mo => mo.Ignore());

        CreateMap<MerchandiseReturnItem, MerchandiseReturnItemDto>();
        CreateMap<MerchandiseReturnShipping, MerchandiseReturnShippingDto>();
        CreateMap<MerchandiseReturnRefund, MerchandiseReturnRefundDto>();
        CreateMap<MerchandiseReturnStatusChange, MerchandiseReturnStatusChangeDto>();
        CreateMap<ReturnDropOffPoint, ReturnDropOffPointDto>();
    }

    public int Order => 1;
}
