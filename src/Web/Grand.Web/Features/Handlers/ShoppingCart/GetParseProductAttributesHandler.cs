using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetParseProductAttributesHandler : IRequestHandler<GetParseProductAttributes, IList<CustomAttribute>>
    {
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IDownloadService _downloadService;
        private readonly IProductService _productService;

        public GetParseProductAttributesHandler(
            IProductAttributeParser productAttributeParser,
            IDownloadService downloadService,
            IProductService productService)
        {
            _productAttributeParser = productAttributeParser;
            _downloadService = downloadService;
            _productService = productService;
        }

        public async Task<IList<CustomAttribute>> Handle(GetParseProductAttributes request, CancellationToken cancellationToken)
        {
            var customAttributes = new List<CustomAttribute>();

            #region Product attributes

            var productAttributes = request.Product.ProductAttributeMappings.ToList();
            if (request.Product.ProductTypeId == ProductType.BundledProduct)
            {
                foreach (var bundle in request.Product.BundleProducts)
                {
                    var bp = await _productService.GetProductById(bundle.ProductId);
                    if (bp.ProductAttributeMappings.Any())
                        productAttributes.AddRange(bp.ProductAttributeMappings);
                }
            }

            foreach (var attribute in productAttributes)
            {
                string controlId = string.Format("product_attribute_{0}", attribute.Id);
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
                                customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
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
                                    if (!String.IsNullOrEmpty(item))
                                        customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
                                            attribute, item).ToList();
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = attribute.ProductAttributeValues;
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
                                    attribute, selectedAttributeId).ToList();
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
                                customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            request.Form.TryGetValue(controlId + "_day", out var day);
                            request.Form.TryGetValue(controlId + "_month", out var month);
                            request.Form.TryGetValue(controlId + "_year", out var year);
                            DateTime? selectedDate = null;
                            try
                            {
                                selectedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                            }
                            catch { }
                            if (selectedDate.HasValue)
                            {
                                customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
                                    attribute, selectedDate.Value.ToString("D")).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            request.Form.TryGetValue(controlId, out var guid);
                            Guid.TryParse(guid, out Guid downloadGuid);
                            var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                customAttributes = _productAttributeParser.AddProductAttribute(customAttributes,
                                        attribute, download.DownloadGuid.ToString()).ToList();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in productAttributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(request.Product, attribute, customAttributes);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    customAttributes = _productAttributeParser.RemoveProductAttribute(customAttributes, attribute).ToList();
                }
            }

            #endregion

            #region Gift vouchers

            if (request.Product.IsGiftVoucher)
            {
                string recipientName = "";
                string recipientEmail = "";
                string senderName = "";
                string senderEmail = "";
                string giftVoucherMessage = "";
                foreach (string formKey in request.Form.Keys)
                {
                    if (formKey.Equals(string.Format("giftvoucher_{0}.RecipientName", request.Product.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        recipientName = request.Form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftvoucher_{0}.RecipientEmail", request.Product.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        recipientEmail = request.Form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftvoucher_{0}.SenderName", request.Product.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        senderName = request.Form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftvoucher_{0}.SenderEmail", request.Product.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        senderEmail = request.Form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftvoucher_{0}.Message", request.Product.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        giftVoucherMessage = request.Form[formKey];
                        continue;
                    }
                }

                customAttributes = _productAttributeParser.AddGiftVoucherAttribute(customAttributes,
                    recipientName, recipientEmail, senderName, senderEmail, giftVoucherMessage).ToList();
            }

            #endregion

            return customAttributes;
        }
    }
}
