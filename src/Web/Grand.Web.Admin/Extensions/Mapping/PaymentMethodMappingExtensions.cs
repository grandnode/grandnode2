using AutoMapper;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Web.Admin.Models.Payments;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class PaymentMethodMappingExtensions
{
    public static async Task<PaymentMethodModel> ToModel(this IMapper mapper, IPaymentProvider entity)
    {
        var paymentMethod = mapper.Map<PaymentMethodModel>(entity);
        paymentMethod.SupportCapture = await entity.SupportCapture();
        paymentMethod.SupportPartiallyRefund = await entity.SupportPartiallyRefund();
        paymentMethod.SupportRefund = await entity.SupportRefund();
        paymentMethod.SupportVoid = await entity.SupportVoid();
        return paymentMethod;
    }
}