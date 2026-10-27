using Grand.Mapping;
using Grand.Domain.Payments;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Payments;

namespace Grand.Web.AdminShared.Mapper;

public class PaymentSettingsProfile : Profile, IAutoMapperProfile
{
    public PaymentSettingsProfile()
    {
        CreateMap<PaymentSettings, PaymentSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.OrderApprovalPaymentMethodSystemNames, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableOrderApprovalPaymentMethods, mo => mo.Ignore());

        CreateMap<PaymentSettingsModel, PaymentSettings>()
            .ForMember(dest => dest.ActivePaymentProviderSystemNames, mo => mo.Ignore());
    }

    public int Order => 0;
}