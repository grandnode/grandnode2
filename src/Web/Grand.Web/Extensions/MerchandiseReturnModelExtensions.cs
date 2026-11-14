using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Extensions;

public static class MerchandiseReturnModelExtensions
{
    /// <summary>
    ///     The return the form asks for, as the submit saves it and the estimate previews it
    /// </summary>
    /// <param name="model">The posted form</param>
    /// <param name="order">The order returned, whose store handles the return</param>
    /// <param name="customer">The customer asking</param>
    /// <param name="groupService">Tells whether the customer is the owner of their sub-accounts</param>
    /// <param name="languageId">The language the reason and action names are kept in</param>
    /// <param name="pickupAddress">The address the items are picked up from</param>
    /// <param name="postedForm">The fields posted, of which the chosen return method's form is read</param>
    /// <param name="orderSettings">The settings of the store</param>
    public static async Task<MerchandiseReturnSubmitRequest> ToSubmitRequest(this MerchandiseReturnModel model,
        Order order, Customer customer, IGroupService groupService, string languageId, Address pickupAddress,
        IDictionary<string, string> postedForm, OrderSettings orderSettings)
    {
        return new MerchandiseReturnSubmitRequest {
            Order = order,
            //the store the order was placed in, whose staff handle its returns
            StoreId = order.StoreId,
            CustomerId = customer.Id,
            OwnerId = await groupService.IsOwner(customer) ? customer.Id : customer.OwnerId,
            LanguageId = languageId,
            Comments = model.Comments,
            PickupAddress = pickupAddress,
            PickupDate = model.PickupDate,
            //a store that does not ask for the requested action saves none, whatever was posted
            Items = model.Items.Select(x => new MerchandiseReturnSubmitItem(x.Id, x.Quantity,
                x.MerchandiseReturnReasonId,
                orderSettings.MerchandiseReturns_HideRequestedAction ? null : x.MerchandiseReturnActionId)).ToList(),
            ReturnShipping = ReturnShippingChoice(model, postedForm)
        };
    }

    /// <summary>
    ///     The return method chosen, with the fields of its form, prefix stripped; null when none was chosen
    /// </summary>
    private static ReturnShippingChoice ReturnShippingChoice(MerchandiseReturnModel model,
        IDictionary<string, string> postedForm)
    {
        if (string.IsNullOrEmpty(model.ReturnShippingProvider))
            return null;

        const string prefix = MerchandiseReturnSubmitCommandHandler.FormPrefix;
        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in postedForm ?? new Dictionary<string, string>())
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && key.Length > prefix.Length)
                form[key[prefix.Length..]] = value;

        return new ReturnShippingChoice {
            ProviderSystemName = model.ReturnShippingProvider,
            OptionCode = model.ReturnShippingOption,
            Form = form
        };
    }
}
