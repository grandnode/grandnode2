﻿using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Admin.Extensions
{
    /// <summary>
    /// Parser helper
    /// </summary>
    public static class AttributeParserHelper
    {
        public static async Task<List<CustomAttribute>> ParseCustomAddressAttributes(this IFormCollection form,
            IAddressAttributeParser addressAttributeParser,
            IAddressAttributeService addressAttributeService)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var customAttributes = new List<CustomAttribute>();
            var attributes = await addressAttributeService.GetAllAddressAttributes();
            foreach (var attribute in attributes)
            {
                string controlId = string.Format("address_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                                    attribute, ctrlAttributes).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            form.TryGetValue(controlId, out var cblAttributes);
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (!string.IsNullOrEmpty(item))
                                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                                            attribute, item).ToList();
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = attribute.AddressAttributeValues;
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                                            attribute, selectedAttributeId).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.ToString().Trim();
                                customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.FileUpload:
                    case AttributeControlType.ImageSquares:
                    //not supported address attributes
                    default:
                        break;
                }
            }

            return customAttributes;
        }
    }
}

