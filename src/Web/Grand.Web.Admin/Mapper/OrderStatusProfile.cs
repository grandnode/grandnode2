using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Mapper
{
    public class OrderStatusProfile : Profile, IAutoMapperProfile
    {
        public OrderStatusProfile()
        {
            CreateMap<OrderStatus, OrderStatusModel>();
            CreateMap<OrderStatusModel, OrderStatus>()
                .ForMember(dest => dest.IsSystem, mo => mo.Ignore())
                .ForMember(dest => dest.StatusId, mo => mo.Ignore())
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}