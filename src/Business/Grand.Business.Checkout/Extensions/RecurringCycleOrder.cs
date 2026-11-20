using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;

namespace Grand.Business.Checkout.Extensions;

public static class RecurringCycleOrder
{
    /// <summary>
    ///     The order that pays one later cycle of a recurring payment: the order the customer placed, at its prices,
    ///     as a new unpaid order. Everything that describes what was bought and what it costs is copied; identity,
    ///     dates, statuses, payment and shipping progress and the loyalty points spent are those of a new order.
    ///     Loyalty points and gift vouchers spent on the initial order are not spent again, so their amounts are added
    ///     back to the total.
    /// </summary>
    /// <param name="giftVouchersAmount">The gift voucher amount redeemed on the initial order</param>
    public static Order Copy(Order initialOrder, string orderId, int cycleNumber, string orderCode,
        DateTime createdOnUtc, double giftVouchersAmount)
    {
        var order = new Order {
            Id = orderId,
            OrderGuid = Guid.NewGuid(),
            // InsertOrder assigns the number
            OrderNumber = 0,
            Code = orderCode,
            InitialOrderId = initialOrder.Id,
            RecurringCycleNumber = cycleNumber,
            CreatedOnUtc = createdOnUtc,
            OrderStatusId = (int)OrderStatusSystem.Pending,
            PaymentStatusId = PaymentStatus.Pending,
            PaidAmount = 0,
            PaidDateUtc = null,
            PaymentDueDateUtc = null,
            PaymentReminderSentOnUtc = null,
            PaymentOverdueNoticeSentOnUtc = null,
            RefundedAmount = 0,
            RedeemedLoyaltyPoints = 0,
            RedeemedLoyaltyPointsAmount = 0,
            LoyaltyPointsWereAdded = false,
            CalcLoyaltyPoints = 0,
            OrderTotal = Math.Round(
                initialOrder.OrderTotal + initialOrder.RedeemedLoyaltyPointsAmount + giftVouchersAmount, 6),
            Deleted = false,
            Imported = false,

            PurchaseOrderNumber = initialOrder.PurchaseOrderNumber,
            StoreId = initialOrder.StoreId,
            CustomerId = initialOrder.CustomerId,
            OwnerId = initialOrder.OwnerId,
            SeId = initialOrder.SeId,
            PickUpInStore = initialOrder.PickUpInStore,
            PaymentMethodSystemName = initialOrder.PaymentMethodSystemName,
            PaymentOptionAttribute = initialOrder.PaymentOptionAttribute,
            CustomerCurrencyCode = initialOrder.CustomerCurrencyCode,
            PrimaryCurrencyCode = initialOrder.PrimaryCurrencyCode,
            CurrencyRate = initialOrder.CurrencyRate,
            Rate = initialOrder.Rate,
            CustomerTaxDisplayTypeId = initialOrder.CustomerTaxDisplayTypeId,
            VatNumber = initialOrder.VatNumber,
            VatNumberStatusId = initialOrder.VatNumberStatusId,
            CompanyName = initialOrder.CompanyName,
            CustomerEmail = initialOrder.CustomerEmail,
            FirstName = initialOrder.FirstName,
            LastName = initialOrder.LastName,
            OrderSubtotalInclTax = initialOrder.OrderSubtotalInclTax,
            OrderSubtotalExclTax = initialOrder.OrderSubtotalExclTax,
            OrderSubTotalDiscountInclTax = initialOrder.OrderSubTotalDiscountInclTax,
            OrderSubTotalDiscountExclTax = initialOrder.OrderSubTotalDiscountExclTax,
            OrderShippingInclTax = initialOrder.OrderShippingInclTax,
            OrderShippingExclTax = initialOrder.OrderShippingExclTax,
            PaymentMethodAdditionalFeeInclTax = initialOrder.PaymentMethodAdditionalFeeInclTax,
            PaymentMethodAdditionalFeeExclTax = initialOrder.PaymentMethodAdditionalFeeExclTax,
            OrderTax = initialOrder.OrderTax,
            OrderDiscount = initialOrder.OrderDiscount,
            CheckoutAttributeDescription = initialOrder.CheckoutAttributeDescription,
            CheckoutAttributes = CopyAttributes(initialOrder.CheckoutAttributes),
            CustomerLanguageId = initialOrder.CustomerLanguageId,
            AffiliateId = initialOrder.AffiliateId,
            CustomerIp = initialOrder.CustomerIp,
            ShippingMethod = initialOrder.ShippingMethod,
            ShippingRateProviderSystemName = initialOrder.ShippingRateProviderSystemName,
            ShippingOptionAttributeDescription = initialOrder.ShippingOptionAttributeDescription,
            ShippingOptionAttribute = initialOrder.ShippingOptionAttribute,
            IsRecurring = initialOrder.IsRecurring,
            RecurringCycleLength = initialOrder.RecurringCycleLength,
            RecurringCyclePeriodId = initialOrder.RecurringCyclePeriodId,
            RecurringTotalCycles = initialOrder.RecurringTotalCycles,
            RecurringUseCycles = initialOrder.RecurringUseCycles,
            BillingAddress = CopyAddress(initialOrder.BillingAddress),
            ShippingAddress = CopyAddress(initialOrder.ShippingAddress),
            PickupPoint = CopyPickupPoint(initialOrder.PickupPoint)
        };

        // the setters of the three collections are protected, so they are filled in place
        foreach (var tax in initialOrder.OrderTaxes)
            order.OrderTaxes.Add(new OrderTax { Percent = tax.Percent, Amount = tax.Amount });

        foreach (var item in initialOrder.OrderItems)
            order.OrderItems.Add(CopyItem(item, createdOnUtc));

        order.ShippingStatusId = order.OrderItems.Any(x => x.IsShipEnabled)
            ? ShippingStatus.Pending
            : ShippingStatus.ShippingNotRequired;

        return order;
    }

    private static OrderItem CopyItem(OrderItem item, DateTime createdOnUtc)
    {
        return new OrderItem {
            OrderItemGuid = Guid.NewGuid(),
            ProductId = item.ProductId,
            Sku = item.Sku,
            VendorId = item.VendorId,
            WarehouseId = item.WarehouseId,
            SeId = item.SeId,
            Quantity = item.Quantity,
            IsShipEnabled = item.IsShipEnabled,
            // nothing of the new order is open, shipped, cancelled or returned yet
            OpenQty = item.Quantity,
            ShipQty = 0,
            CancelQty = 0,
            CancelAmount = 0,
            ReturnQty = 0,
            Status = OrderItemStatus.Open,
            TaxRate = item.TaxRate,
            UnitPriceWithoutDiscInclTax = item.UnitPriceWithoutDiscInclTax,
            UnitPriceWithoutDiscExclTax = item.UnitPriceWithoutDiscExclTax,
            UnitPriceInclTax = item.UnitPriceInclTax,
            UnitPriceExclTax = item.UnitPriceExclTax,
            PriceInclTax = item.PriceInclTax,
            PriceExclTax = item.PriceExclTax,
            DiscountAmountInclTax = item.DiscountAmountInclTax,
            DiscountAmountExclTax = item.DiscountAmountExclTax,
            OriginalProductCost = item.OriginalProductCost,
            AttributeDescription = item.AttributeDescription,
            Attributes = CopyAttributes(item.Attributes),
            DownloadCount = 0,
            IsDownloadActivated = false,
            LicenseDownloadId = "",
            ItemWeight = item.ItemWeight,
            RentalStartDateUtc = item.RentalStartDateUtc,
            RentalEndDateUtc = item.RentalEndDateUtc,
            CreatedOnUtc = createdOnUtc,
            Commission = item.Commission,
            cId = item.cId
        };
    }

    private static IList<CustomAttribute> CopyAttributes(IList<CustomAttribute> attributes)
    {
        return attributes?.Select(x => new CustomAttribute { Key = x.Key, Value = x.Value }).ToList();
    }

    private static Address CopyAddress(Address address)
    {
        if (address == null) return null;
        return new Address {
            Id = address.Id,
            Name = address.Name,
            FirstName = address.FirstName,
            LastName = address.LastName,
            Email = address.Email,
            Company = address.Company,
            VatNumber = address.VatNumber,
            CountryId = address.CountryId,
            StateProvinceId = address.StateProvinceId,
            City = address.City,
            Address1 = address.Address1,
            Address2 = address.Address2,
            ZipPostalCode = address.ZipPostalCode,
            PhoneNumber = address.PhoneNumber,
            FaxNumber = address.FaxNumber,
            Note = address.Note,
            Attributes = CopyAttributes(address.Attributes),
            AddressType = address.AddressType
        };
    }

    private static PickupPoint CopyPickupPoint(PickupPoint point)
    {
        if (point == null) return null;
        return new PickupPoint {
            Id = point.Id,
            UserFields = point.UserFields?.Select(x => new UserField { Key = x.Key, Value = x.Value }).ToList(),
            CreatedOnUtc = point.CreatedOnUtc,
            CreatedBy = point.CreatedBy,
            UpdatedOnUtc = point.UpdatedOnUtc,
            UpdatedBy = point.UpdatedBy,
            Name = point.Name,
            Description = point.Description,
            AdminComment = point.AdminComment,
            Address = CopyAddress(point.Address),
            WarehouseId = point.WarehouseId,
            StoreId = point.StoreId,
            PickupFee = point.PickupFee,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
            DisplayOrder = point.DisplayOrder
        };
    }
}
