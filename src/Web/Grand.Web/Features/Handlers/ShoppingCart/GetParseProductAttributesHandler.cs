using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Features.Handlers.ShoppingCart;

public class GetParseProductAttributesHandler : IRequestHandler<GetParseProductAttributes, IList<CustomAttribute>>
{
    private readonly IDownloadService _downloadService;
    private readonly IProductService _productService;

    public GetParseProductAttributesHandler(
        IDownloadService downloadService,
        IProductService productService)
    {
        _downloadService = downloadService;
        _productService = productService;
    }

    public async Task<IList<CustomAttribute>> Handle(GetParseProductAttributes request,
        CancellationToken cancellationToken)
    {
        var customAttributes = new List<CustomAttribute>();

        #region Product attributes

        var productAttributes = request.Product.ProductAttributeMappings.ToList();
        if (request.Product.ProductTypeId == ProductType.BundledProduct)
            foreach (var bundle in request.Product.BundleProducts)
            {
                var bp = await _productService.GetProductById(bundle.ProductId);
                if (bp.ProductAttributeMappings.Any())
                    productAttributes.AddRange(bp.ProductAttributeMappings);
            }

        foreach (var attribute in productAttributes)
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    var ctrlAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            if (!string.IsNullOrEmpty(item))
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, item).ToList();
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
                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Datepicker:
                {
                    var ctrlAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                            attribute, enteredText).ToList();
                    }
                }
                    break;
                case AttributeControlType.FileUpload:
                {
                    var guid = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    Guid.TryParse(guid, out var downloadGuid);
                    var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                    if (download != null)
                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                            attribute, download.DownloadGuid.ToString()).ToList();
                }
                    break;
            }

        //validate conditional attributes (if specified)
        foreach (var attribute in productAttributes)
        {
            var conditionMet = request.Product.IsConditionMet(attribute, customAttributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customAttributes = ProductExtensions.RemoveProductAttribute(customAttributes, attribute).ToList();
        }

        #endregion

        return customAttributes;
    }
}