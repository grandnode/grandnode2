namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Why a merchandise return was not changed, as resource keys. The panel keys go without the panel's prefix
///     (Admin. or Vendor.)
/// </summary>
public static class MerchandiseReturnErrors
{
    public const string NotFound = "Orders.MerchandiseReturns.NotFound";
    public const string StatusChangeNotAllowed = "Orders.MerchandiseReturns.StatusChangeNotAllowed";
    public const string QuantityExceeded = "Orders.MerchandiseReturns.QuantityExceeded";
    public const string AlreadyReceived = "Orders.MerchandiseReturns.AlreadyReceived";
    public const string Busy = "Orders.MerchandiseReturns.Busy";
    public const string LabelNotAllowed = "Orders.MerchandiseReturns.LabelNotAllowed";
    public const string ShippingChangeNotAllowed = "Orders.MerchandiseReturns.ShippingChangeNotAllowed";
    public const string RefundNotAllowed = "Orders.MerchandiseReturns.Refund.NotAllowed";
    public const string RefundAmountNotAllowed = "Orders.MerchandiseReturns.Refund.AmountNotAllowed";
    public const string NothingToRefund = "Orders.MerchandiseReturns.Refund.NothingToRefund";
    public const string AlreadyRefunded = "Orders.MerchandiseReturns.Refund.AlreadyRefunded";
    public const string RefundInProgress = "Orders.MerchandiseReturns.Refund.InProgress";
    public const string RefundUnconfirmed = "Orders.MerchandiseReturns.Refund.Unconfirmed";

    //the storefront's keys, as they are
    public const string SubmitNothingToReturn = "MerchandiseReturns.NoItemsSubmitted";
    public const string SubmitQuantityExceeded = "MerchandiseReturns.QuantityExceeded";
    public const string SubmitBusy = "MerchandiseReturns.Busy";
    public const string SubmitReturnShippingChoose = "MerchandiseReturns.ReturnShipping.Choose";
}
