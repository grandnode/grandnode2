using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Services.ExportImpot;

public class OrderSchemaProperty : ISchemaProperty<Order>
{
    private readonly ICountryService _countryService;

    public OrderSchemaProperty(ICountryService countryService)
    {
        _countryService = countryService;
    }

    public virtual async Task<PropertyByName<Order>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Order>("OrderNumber", p => p.OrderNumber),
            new PropertyByName<Order>("OrderCode", p => p.Code),
            new PropertyByName<Order>("OrderId", p => p.Id),
            new PropertyByName<Order>("StoreId", p => p.StoreId),
            new PropertyByName<Order>("OrderGuid", p => p.OrderGuid),
            new PropertyByName<Order>("CustomerId", p => p.CustomerId),
            new PropertyByName<Order>("OrderStatusId", p => p.OrderStatusId),
            new PropertyByName<Order>("PaymentStatusId", p => p.PaymentStatusId),
            new PropertyByName<Order>("ShippingStatusId", p => p.ShippingStatusId),
            new PropertyByName<Order>("OrderSubtotalInclTax", p => p.OrderSubtotalInclTax),
            new PropertyByName<Order>("OrderSubtotalExclTax", p => p.OrderSubtotalExclTax),
            new PropertyByName<Order>("OrderSubTotalDiscountInclTax", p => p.OrderSubTotalDiscountInclTax),
            new PropertyByName<Order>("OrderSubTotalDiscountExclTax", p => p.OrderSubTotalDiscountExclTax),
            new PropertyByName<Order>("OrderShippingInclTax", p => p.OrderShippingInclTax),
            new PropertyByName<Order>("OrderShippingExclTax", p => p.OrderShippingExclTax),
            new PropertyByName<Order>("PaymentMethodAdditionalFeeInclTax", p => p.PaymentMethodAdditionalFeeInclTax),
            new PropertyByName<Order>("PaymentMethodAdditionalFeeExclTax", p => p.PaymentMethodAdditionalFeeExclTax),
            new PropertyByName<Order>("OrderTax", p => p.OrderTax),
            new PropertyByName<Order>("OrderTotal", p => p.OrderTotal),
            new PropertyByName<Order>("RefundedAmount", p => p.RefundedAmount),
            new PropertyByName<Order>("OrderDiscount", p => p.OrderDiscount),
            new PropertyByName<Order>("CurrencyRate", p => p.CurrencyRate),
            new PropertyByName<Order>("CustomerCurrencyCode", p => p.CustomerCurrencyCode),
            new PropertyByName<Order>("AffiliateId", p => p.AffiliateId),
            new PropertyByName<Order>("PaymentMethodSystemName", p => p.PaymentMethodSystemName),
            new PropertyByName<Order>("ShippingPickUpInStore", p => p.PickUpInStore),
            new PropertyByName<Order>("ShippingMethod", p => p.ShippingMethod),
            new PropertyByName<Order>("ShippingRateComputationMethodSystemName", p => p.ShippingRateProviderSystemName),
            new PropertyByName<Order>("VatNumber", p => p.VatNumber),
            new PropertyByName<Order>("CreatedOnUtc", p => p.CreatedOnUtc.ToOADate()),
            new PropertyByName<Order>("BillingFirstName", p => p.BillingAddress?.FirstName),
            new PropertyByName<Order>("BillingLastName", p => p.BillingAddress?.LastName),
            new PropertyByName<Order>("BillingEmail", p => p.BillingAddress?.Email),
            new PropertyByName<Order>("BillingCompany", p => p.BillingAddress?.Company),
            new PropertyByName<Order>("BillingVatNumber", p => p.BillingAddress?.VatNumber),
            new PropertyByName<Order>("BillingCountry",
                p => p.BillingAddress != null
                    ? _countryService.GetCountryById(p.BillingAddress.CountryId).Result?.Name
                    : ""),
            new PropertyByName<Order>("BillingCity", p => p.BillingAddress?.City),
            new PropertyByName<Order>("BillingAddress1", p => p.BillingAddress?.Address1),
            new PropertyByName<Order>("BillingAddress2", p => p.BillingAddress?.Address2),
            new PropertyByName<Order>("BillingZipPostalCode", p => p.BillingAddress?.ZipPostalCode),
            new PropertyByName<Order>("BillingPhoneNumber", p => p.BillingAddress?.PhoneNumber),
            new PropertyByName<Order>("BillingFaxNumber", p => p.BillingAddress?.FaxNumber),
            new PropertyByName<Order>("ShippingFirstName", p => p.ShippingAddress?.FirstName),
            new PropertyByName<Order>("ShippingLastName", p => p.ShippingAddress?.LastName),
            new PropertyByName<Order>("ShippingEmail", p => p.ShippingAddress?.Email),
            new PropertyByName<Order>("ShippingCompany", p => p.ShippingAddress?.Company),
            new PropertyByName<Order>("ShippingVatNumber", p => p.ShippingAddress?.VatNumber),
            new PropertyByName<Order>("ShippingCountry",
                p => p.ShippingAddress != null
                    ? _countryService.GetCountryById(p.ShippingAddress.CountryId).Result?.Name
                    : ""),
            new PropertyByName<Order>("ShippingCity", p => p.ShippingAddress?.City),
            new PropertyByName<Order>("ShippingAddress1", p => p.ShippingAddress?.Address1),
            new PropertyByName<Order>("ShippingAddress2", p => p.ShippingAddress?.Address2),
            new PropertyByName<Order>("ShippingZipPostalCode", p => p.ShippingAddress?.ZipPostalCode),
            new PropertyByName<Order>("ShippingPhoneNumber", p => p.ShippingAddress?.PhoneNumber),
            new PropertyByName<Order>("ShippingFaxNumber", p => p.ShippingAddress?.FaxNumber)
        };
        return await Task.FromResult(properties);
    }
}