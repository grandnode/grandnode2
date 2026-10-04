using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class IShippingRateComputationMethodMappingExtensions
{
    public static ShippingRateComputationMethodModel ToModel(this IShippingRateCalculationProvider entity)
    {
        return entity.MapTo<IShippingRateCalculationProvider, ShippingRateComputationMethodModel>();
    }
}