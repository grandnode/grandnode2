using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Commands.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Commands.Handler.ShoppingCart;
public class SaveCheckoutAttributesCommandHandler : IRequestHandler<SaveCheckoutAttributesCommand, IList<CustomAttribute>>
{
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly ICheckoutAttributeService _checkoutAttributeService;
    private readonly ICustomerService _customerService;

    public SaveCheckoutAttributesCommandHandler(
        ICheckoutAttributeService checkoutAttributeService,
        ICheckoutAttributeParser checkoutAttributeParser,
        ICustomerService customerService)
    {
        _checkoutAttributeService = checkoutAttributeService;
        _checkoutAttributeParser = checkoutAttributeParser;
        _customerService = customerService;
    }

    public async Task<IList<CustomAttribute>> Handle(SaveCheckoutAttributesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Cart);
        ArgumentNullException.ThrowIfNull(request.SelectedAttributes);

        var customAttributes = new List<CustomAttribute>();
        var checkoutAttributes =
            await _checkoutAttributeService.GetAllCheckoutAttributes(request.Store.Id,
                !request.Cart.RequiresShipping());
        foreach (var attribute in checkoutAttributes)
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                    {
                        var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                            customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                attribute, ctrlAttributes).ToList();
                    }
                    break;
                case AttributeControlType.Checkboxes:
                    {
                        var cblAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(cblAttributes))
                            foreach (var item in cblAttributes.Split(','))
                                if (!string.IsNullOrEmpty(item))
                                    customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                        attribute, item).ToList();
                    }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                    {
                        //load read-only (already server-side selected) values
                        var attributeValues = attribute.CheckoutAttributeValues;
                        foreach (var selectedAttributeId in attributeValues
                                     .Where(v => v.IsPreSelected)
                                     .Select(v => v.Id)
                                     .ToList())
                            customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                attribute, selectedAttributeId).ToList();
                    }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Datepicker:
                case AttributeControlType.FileUpload:
                    {
                        var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                        {
                            var enteredText = ctrlAttributes.Trim();
                            customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes, attribute, enteredText).ToList();
                        }
                    }
                    break;
            }

        //save checkout attributes
        //validate conditional attributes (if specified)
        foreach (var attribute in checkoutAttributes)
        {
            var conditionMet = await _checkoutAttributeParser.IsConditionMet(attribute, customAttributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customAttributes = _checkoutAttributeParser.RemoveCheckoutAttribute(customAttributes, attribute).ToList();
        }

        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.CheckoutAttributes,
            customAttributes, request.Store.Id);

        return customAttributes;
    }
}