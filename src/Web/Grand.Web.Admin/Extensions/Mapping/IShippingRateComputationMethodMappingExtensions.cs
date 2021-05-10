using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Extensions
{
    public static class IShippingRateComputationMethodMappingExtensions
    {
        public static ShippingRateComputationMethodModel ToModel(this IShippingRateCalculationProvider entity)
        {
            return entity.MapTo<IShippingRateCalculationProvider, ShippingRateComputationMethodModel>();
        }
    }
}