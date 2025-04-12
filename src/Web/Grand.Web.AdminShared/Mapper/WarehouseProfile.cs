using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Mapper;

public class WarehouseProfile : Profile, IAutoMapperProfile
{
    public WarehouseProfile()
    {
        CreateMap<Warehouse, WarehouseModel>()
            .ForMember(dest => dest.Address, mo => mo.MapFrom(y => y.Address));

        CreateMap<WarehouseModel, Warehouse>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}