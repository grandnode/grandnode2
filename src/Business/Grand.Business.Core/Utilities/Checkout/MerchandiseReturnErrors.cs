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

    //the storefront's keys, as they are
    public const string SubmitNothingToReturn = "MerchandiseReturns.NoItemsSubmitted";
    public const string SubmitQuantityExceeded = "MerchandiseReturns.QuantityExceeded";
    public const string SubmitBusy = "MerchandiseReturns.Busy";
    public const string SubmitReturnShippingChoose = "MerchandiseReturns.ReturnShipping.Choose";
}
