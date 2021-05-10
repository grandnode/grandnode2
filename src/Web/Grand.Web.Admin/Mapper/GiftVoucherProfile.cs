using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Mapper
{
    public class GiftVoucherProfile : Profile, IAutoMapperProfile
    {
        public GiftVoucherProfile()
        {
            CreateMap<GiftVoucher, GiftVoucherModel>()
                .ForMember(dest => dest.PurchasedWithOrderId, mo => mo.Ignore())
                .ForMember(dest => dest.AmountStr, mo => mo.Ignore())
                .ForMember(dest => dest.RemainingAmountStr, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore());
            CreateMap<GiftVoucherModel, GiftVoucher>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.GiftVoucherUsageHistory, mo => mo.Ignore())
                .ForMember(dest => dest.PurchasedWithOrderItem, mo => mo.Ignore())
                .ForMember(dest => dest.IsRecipientNotified, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}