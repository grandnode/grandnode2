using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartItemAttributeValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem,
    bool IgnoreNonCombinableAttributes);

public class ShoppingCartItemAttributeValidator : AbstractValidator<ShoppingCartItemAttributeValidatorRecord>
{
    private readonly IProductAttributeService _productAttributeService;
    private readonly IProductService _productService;
    private readonly ITranslationService _translationService;

    public ShoppingCartItemAttributeValidator(ITranslationService translationService, IProductService productService,
        IProductAttributeService productAttributeService)
    {
        _translationService = translationService;
        _productService = productService;
        _productAttributeService = productAttributeService;

        RuleFor(x => x).CustomAsync(async (value, context, ct) =>
        {
            var warnings = await GetShoppingCartItemWarnings(value);
            if (warnings.Any())
            {
                warnings.ToList().ForEach(context.AddFailure);
                return;
            }


            //validate bundled products
            var attributeValues = value.Product.ParseProductAttributeValues(value.ShoppingCartItem.Attributes);
            foreach (var attributeValue in attributeValues)
            {
                var productAttributeMapping =
                    value.Product.ProductAttributeMappings.FirstOrDefault(x =>
                        x.ProductAttributeValues.Any(z => z.Id == attributeValue.Id));
                if (attributeValue.AttributeValueTypeId != AttributeValueType.AssociatedToProduct ||
                    productAttributeMapping == null) continue;
                {
                    if (value.IgnoreNonCombinableAttributes && productAttributeMapping.IsNonCombinable())
                        continue;

                    //associated product (bundle)
                    var associatedProduct = await productService.GetProductById(attributeValue.AssociatedProductId);
                    if (associatedProduct != null)
                    {
                        var totalQty = value.ShoppingCartItem.Quantity * attributeValue.Quantity;
                        var associatedProductWarnings = await GetShoppingCartItemWarnings(
                            value with {
                                Product = associatedProduct, ShoppingCartItem = new ShoppingCartItem {
                                    ShoppingCartTypeId = value.ShoppingCartItem.ShoppingCartTypeId,
                                    StoreId = value.ShoppingCartItem.Id,
                                    Quantity = totalQty,
                                    WarehouseId = value.ShoppingCartItem.WarehouseId,
                                    Attributes = value.ShoppingCartItem.Attributes
                                }
                            });
                        foreach (var associatedProductWarning in associatedProductWarnings)
                        {
                            var productAttribute =
                                await productAttributeService.GetProductAttributeById(productAttributeMapping
                                    .ProductAttributeId);
                            var attributeName = productAttribute.Name;
                            var attributeValueName = attributeValue.Name;
                            warnings.Add(string.Format(
                                translationService.GetResource("ShoppingCart.AssociatedAttributeWarning"),
                                attributeName, attributeValueName, associatedProductWarning));
                        }
                    }
                    else
                    {
                        warnings.Add($"Associated product cannot be loaded - {attributeValue.AssociatedProductId}");
                    }

                    if (!warnings.Any()) continue;
                    warnings.ToList().ForEach(context.AddFailure);
                    return;
                }
            }
        });
    }


    private async Task<IList<string>> GetShoppingCartItemWarnings(ShoppingCartItemAttributeValidatorRecord value)
    {
        //ensure it's our attributes
        var warnings = new List<string>();

        var attributes1 = value.Product.ParseProductAttributeMappings(value.ShoppingCartItem.Attributes).ToList();
        if (value.Product.ProductTypeId == ProductType.BundledProduct)
            foreach (var bundle in value.Product.BundleProducts)
            {
                var p1 = await _productService.GetProductById(bundle.ProductId);
                if (p1 != null)
                {
                    var a1 = p1.ParseProductAttributeMappings(value.ShoppingCartItem.Attributes).ToList();
                    attributes1.AddRange(a1);
                }
            }

        if (value.IgnoreNonCombinableAttributes) attributes1 = attributes1.Where(x => !x.IsNonCombinable()).ToList();

        //foreach (var attribute in attributes1)
        //{
        //    if (string.IsNullOrEmpty(attribute.ProductId))
        //    {
        //        warnings.Add("Attribute error");
        //        return warnings;
        //    }
        //}

        //validate required product attributes (whether they're chosen/selected/entered)
        var attributes2 = value.Product.ProductAttributeMappings.ToList();
        if (value.Product.ProductTypeId == ProductType.BundledProduct)
            foreach (var bundle in value.Product.BundleProducts)
            {
                var p1 = await _productService.GetProductById(bundle.ProductId);
                if (p1 != null && p1.ProductAttributeMappings.Any()) attributes2.AddRange(p1.ProductAttributeMappings);
            }

        if (value.IgnoreNonCombinableAttributes) attributes2 = attributes2.Where(x => !x.IsNonCombinable()).ToList();
        //validate conditional attributes only (if specified)
        attributes2 = attributes2.Where(x =>
        {
            var conditionMet = value.Product.IsConditionMet(x, value.ShoppingCartItem.Attributes);
            return !conditionMet.HasValue || conditionMet.Value;
        }).ToList();
        foreach (var a2 in attributes2)
        {
            if (a2.IsRequired)
            {
                var found = false;
                //selected product attributes
                foreach (var attributeValuesStr in from a1 in attributes1
                         where a1.Id == a2.Id
                         select ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, a1.Id)
                         into attributeValuesStr
                         where attributeValuesStr.Any(str1 => !string.IsNullOrEmpty(str1.Trim()))
                         select attributeValuesStr)
                    found = true;

                //if not found
                if (!found)
                {
                    var paa = await _productAttributeService.GetProductAttributeById(a2.ProductAttributeId);
                    if (paa != null)
                    {
                        var notFoundWarning = !string.IsNullOrEmpty(a2.TextPrompt)
                            ? a2.TextPrompt
                            : string.Format(_translationService.GetResource("ShoppingCart.SelectAttribute"), paa.Name);

                        warnings.Add(notFoundWarning);
                    }
                }
            }

            if (a2.AttributeControlTypeId == AttributeControlType.ReadonlyCheckboxes)
            {
                //customers cannot edit read-only attributes
                var allowedReadOnlyValueIds = a2.ProductAttributeValues
                    .Where(x => x.IsPreSelected)
                    .Select(x => x.Id)
                    .ToArray();

                var selectedReadOnlyValueIds = value.Product
                    .ParseProductAttributeValues(value.ShoppingCartItem.Attributes)
                    //.Where(x => x.ProductAttributeMappingId == a2.Id)
                    .Select(x => x.Id)
                    .ToArray();

                if (!CommonHelper.ArraysEqual(allowedReadOnlyValueIds, selectedReadOnlyValueIds))
                    warnings.Add("You cannot change read-only values");
            }
        }

        //validation rules
        foreach (var pam in attributes2)
        {
            if (!pam.ValidationRulesAllowed())
                continue;

            //minimum length
            if (pam.ValidationMinLength.HasValue)
                if (pam.AttributeControlTypeId is AttributeControlType.TextBox or AttributeControlType.MultilineTextbox)
                {
                    var valuesStr = ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, pam.Id);
                    var enteredText = valuesStr.FirstOrDefault();
                    var enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                    if (pam.ValidationMinLength.Value > enteredTextLength)
                    {
                        var productAttribute =
                            await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMinimumLength"),
                            productAttribute.Name, pam.ValidationMinLength.Value));
                    }
                }

            //maximum length
            if (!pam.ValidationMaxLength.HasValue) continue;
            {
                if (pam.AttributeControlTypeId != AttributeControlType.TextBox &&
                    pam.AttributeControlTypeId != AttributeControlType.MultilineTextbox) continue;
                var valuesStr = ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, pam.Id);
                var enteredText = valuesStr.FirstOrDefault();
                var enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                if (pam.ValidationMaxLength.Value >= enteredTextLength) continue;
                var productAttribute = await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMaximumLength"),
                    productAttribute.Name, pam.ValidationMaxLength.Value));
            }
        }

        return warnings;
    }
}