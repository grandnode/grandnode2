using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class OrderSettingsProfile : Profile, IAutoMapperProfile
    {
        public OrderSettingsProfile()
        {
            CreateMap<OrderSettings, SalesSettingsModel.OrderSettingsModel>()
                .ForMember(dest => dest.GiftVouchers_Activated_OrderStatuses, mo => mo.Ignore())
                .ForMember(dest => dest.PrimaryStoreCurrencyCode, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<SalesSettingsModel.OrderSettingsModel, OrderSettings>()
                .ForMember(dest => dest.MinimumOrderPlacementInterval, mo => mo.Ignore())
                .ForMember(dest => dest.UnpublishAuctionProduct, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}