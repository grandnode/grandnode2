using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Payments;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Extensions
{
    public static class IPaymentMethodMappingExtensions
    {
        public static async Task<PaymentMethodModel> ToModel(this IPaymentProvider entity)
        {
            var paymentmethod = entity.MapTo<IPaymentProvider, PaymentMethodModel>();

            paymentmethod.SupportCapture = await entity.SupportCapture();
            paymentmethod.SupportPartiallyRefund = await entity.SupportPartiallyRefund();
            paymentmethod.SupportRefund = await entity.SupportRefund();
            paymentmethod.SupportVoid = await entity.SupportVoid();

            return paymentmethod;
        }
    }
}