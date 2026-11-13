using Grand.Domain.Orders;

namespace Grand.Business.Core.Extensions;

public static class MerchandiseReturnShippingExtensions
{
    /// <summary>
    ///     The system name of the built-in method where the customer sends the parcel with a carrier of their own
    /// </summary>
    public const string SendYourselfSystemName = "ReturnShipping.SendYourself";

    /// <summary>
    ///     Whether the customer types in the tracking number: they chose to send the parcel themselves, and the
    ///     return is not received, rejected or cancelled yet
    /// </summary>
    public static bool CustomerEntersTrackingNumber(this MerchandiseReturn merchandiseReturn)
    {
        return string.Equals(merchandiseReturn?.ReturnShipping?.ProviderSystemName, SendYourselfSystemName,
                   StringComparison.OrdinalIgnoreCase)
               && merchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
                   or MerchandiseReturnStatus.Accepted;
    }

    /// <summary>
    ///     Whether the customer is told how to send the parcel back (the instructions, the return code, the label):
    ///     the return is not received, rejected or cancelled yet, and its label was not cancelled
    /// </summary>
    public static bool CustomerSendsBack(this MerchandiseReturn merchandiseReturn)
    {
        return merchandiseReturn?.ReturnShipping != null
               && merchandiseReturn.ReturnShipping.LabelStatus != ReturnLabelStatus.Cancelled
               && merchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
                   or MerchandiseReturnStatus.Accepted;
    }

    /// <summary>
    ///     Whether the customer can ask for the label again: it failed, and the return is not received, rejected or
    ///     cancelled yet
    /// </summary>
    public static bool CustomerRetriesLabel(this MerchandiseReturn merchandiseReturn)
    {
        return merchandiseReturn?.ReturnShipping?.LabelStatus == ReturnLabelStatus.Failed
               && merchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
                   or MerchandiseReturnStatus.Accepted;
    }

    /// <summary>
    ///     What the customer pays to ship a merchandise return: nothing when the store's fees are free or when any
    ///     reason ships free; otherwise the cost of the chosen option. The requested action never changes it
    /// </summary>
    public static double CustomerCost(double optionCost, MerchandiseReturnFees fees, bool anyFreeReason)
    {
        if (fees == MerchandiseReturnFees.Free) return 0;
        if (anyFreeReason) return 0;
        return optionCost;
    }
}
