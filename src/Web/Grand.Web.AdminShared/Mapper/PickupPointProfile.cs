using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Mapper;

public class PickupPointProfile : Profile, IAutoMapperProfile
{
    public PickupPointProfile()
    {
        CreateMap<PickupPoint, PickupPointModel>()
            .ForMember(dest => dest.Address, mo => mo.MapFrom(y => y.Address));

        CreateMap<PickupPointModel, PickupPoint>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}