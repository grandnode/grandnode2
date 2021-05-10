using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.ShoppingCart;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.ShoppingCart
{
    public class SaveCheckoutAttributesCommandHandler : IRequestHandler<SaveCheckoutAttributesCommand, IList<CustomAttribute>>
    {
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IDownloadService _downloadService;
        private readonly IUserFieldService _userFieldService;

        public SaveCheckoutAttributesCommandHandler(
            ICheckoutAttributeService checkoutAttributeService,
            ICheckoutAttributeParser checkoutAttributeParser,
            IDownloadService downloadService,
            IUserFieldService userFieldService)
        {
            _checkoutAttributeService = checkoutAttributeService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _downloadService = downloadService;
            _userFieldService = userFieldService;
        }

        public async Task<IList<CustomAttribute>> Handle(SaveCheckoutAttributesCommand request, CancellationToken cancellationToken)
        {
            if (request.Cart == null)
                throw new ArgumentNullException(nameof(request.Cart));

            if (request.Form == null)
                throw new ArgumentNullException(nameof(request.Form));

            var customAttributes = new List<CustomAttribute>();
            var checkoutAttributes = await _checkoutAttributeService.GetAllCheckoutAttributes(request.Store.Id, !request.Cart.RequiresShipping());
            foreach (var attribute in checkoutAttributes)
            {
                string controlId = string.Format("checkout_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                        attribute, ctrlAttributes).ToList();

                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            request.Form.TryGetValue(controlId, out var cblAttributes);
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes)
                                {
                                    customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes, attribute, item).ToList();
                                }
                            }
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
                            {
                                customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                            attribute, selectedAttributeId.ToString()).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.ToString().Trim();
                                customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            request.Form.TryGetValue(controlId + "_day", out var date);
                            request.Form.TryGetValue(controlId + "_month", out var month);
                            request.Form.TryGetValue(controlId + "_year", out var year);
                            DateTime? selectedDate = null;
                            try
                            {
                                selectedDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(date));
                            }
                            catch { }
                            if (selectedDate.HasValue)
                            {
                                customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                    attribute, selectedDate.Value.ToString("D")).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            Guid downloadGuid;
                            request.Form.TryGetValue(controlId, out var guid);

                            Guid.TryParse(guid, out downloadGuid);
                            var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                customAttributes = _checkoutAttributeParser.AddCheckoutAttribute(customAttributes,
                                           attribute, download.DownloadGuid.ToString()).ToList();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            //save checkout attributes
            //validate conditional attributes (if specified)
            foreach (var attribute in checkoutAttributes)
            {
                var conditionMet = await _checkoutAttributeParser.IsConditionMet(attribute, customAttributes);
                if (conditionMet.HasValue && !conditionMet.Value)
                    customAttributes = _checkoutAttributeParser.RemoveCheckoutAttribute(customAttributes, attribute).ToList();
            }
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.CheckoutAttributes, customAttributes, request.Store.Id);

            return customAttributes;
        }
    }
}
