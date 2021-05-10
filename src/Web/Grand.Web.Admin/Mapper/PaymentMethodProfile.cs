using AutoMapper;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Payments;

namespace Grand.Web.Admin.Mapper
{
    public class PaymentMethodProfile : Profile, IAutoMapperProfile
    {
        public PaymentMethodProfile()
        {
            CreateMap<IPaymentProvider, PaymentMethodModel>()
                .ForMember(dest => dest.FriendlyName, mo => mo.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.SystemName, mo => mo.MapFrom(src => src.SystemName))
                .ForMember(dest => dest.DisplayOrder, mo => mo.MapFrom(src => src.Priority))
                .ForMember(dest => dest.SupportCapture, mo => mo.Ignore())
                .ForMember(dest => dest.SupportPartiallyRefund, mo => mo.Ignore())
                .ForMember(dest => dest.SupportRefund, mo => mo.Ignore())
                .ForMember(dest => dest.SupportVoid, mo => mo.Ignore())
                .ForMember(dest => dest.IsActive, mo => mo.Ignore())
                .ForMember(dest => dest.LogoUrl, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}