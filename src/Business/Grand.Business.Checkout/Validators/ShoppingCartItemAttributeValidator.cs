using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Checkout.Validators
{

    public record ShoppingCartItemAttributeValidatorRecord(Customer Customer, Product Product, ShoppingCartItem ShoppingCartItem, bool IgnoreNonCombinableAttributes);

    public class ShoppingCartItemAttributeValidator : AbstractValidator<ShoppingCartItemAttributeValidatorRecord>
    {
        private readonly ITranslationService _translationService;
        private readonly IAclService _aclService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;

        public ShoppingCartItemAttributeValidator(ITranslationService translationService, IAclService aclService, IProductService productService, IProductAttributeService productAttributeService)
        {
            _translationService = translationService;
            _aclService = aclService;
            _productService = productService;
            _productAttributeService = productAttributeService;

            RuleFor(x => x).CustomAsync(async (value, context, ct) =>
            {
                var warnings = await GetShoppingCartItemWarnings(value);
                if (warnings.Any())
                {
                    warnings.ToList().ForEach(x => context.AddFailure(x));
                    return;
                }


                //validate bundled products
                var attributeValues = value.Product.ParseProductAttributeValues(value.ShoppingCartItem.Attributes);
                foreach (var attributeValue in attributeValues)
                {
                    var _productAttributeMapping = value.Product.ProductAttributeMappings.Where(x => x.ProductAttributeValues.Any(z => z.Id == attributeValue.Id)).FirstOrDefault();
                    //TODO - check value.Product.ProductAttributeMappings.Where(x => x.Id == attributeValue.ProductAttributeMappingId).FirstOrDefault();
                    if (attributeValue.AttributeValueTypeId == AttributeValueType.AssociatedToProduct && _productAttributeMapping != null)
                    {
                        if (value.IgnoreNonCombinableAttributes && _productAttributeMapping.IsNonCombinable())
                            continue;

                        //associated product (bundle)
                        var associatedProduct = await productService.GetProductById(attributeValue.AssociatedProductId);
                        if (associatedProduct != null)
                        {
                            var totalQty = value.ShoppingCartItem.Quantity * attributeValue.Quantity;
                            var associatedProductWarnings = await GetShoppingCartItemWarnings(
                                new ShoppingCartItemAttributeValidatorRecord(value.Customer, associatedProduct, new ShoppingCartItem() {
                                    ShoppingCartTypeId = value.ShoppingCartItem.ShoppingCartTypeId,
                                    StoreId = value.ShoppingCartItem.Id,
                                    Quantity = totalQty,
                                    WarehouseId = value.ShoppingCartItem.WarehouseId,
                                    Attributes = value.ShoppingCartItem.Attributes
                                }, value.IgnoreNonCombinableAttributes));
                            foreach (var associatedProductWarning in associatedProductWarnings)
                            {
                                var productAttribute = await productAttributeService.GetProductAttributeById(_productAttributeMapping.ProductAttributeId);
                                var attributeName = productAttribute.Name;
                                var attributeValueName = attributeValue.Name;
                                warnings.Add(string.Format(
                                    translationService.GetResource("ShoppingCart.AssociatedAttributeWarning"),
                                    attributeName, attributeValueName, associatedProductWarning));
                            }
                        }
                        else
                        {
                            warnings.Add(string.Format("Associated product cannot be loaded - {0}", attributeValue.AssociatedProductId));
                        }

                        if (warnings.Any())
                        {
                            warnings.ToList().ForEach(x => context.AddFailure(x));
                            return;
                        }
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
            {
                foreach (var bundle in value.Product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 != null)
                    {
                        var a1 = p1.ParseProductAttributeMappings(value.ShoppingCartItem.Attributes).ToList();
                        attributes1.AddRange(a1);
                    }
                }

            }
            if (value.IgnoreNonCombinableAttributes)
            {
                attributes1 = attributes1.Where(x => !x.IsNonCombinable()).ToList();

            }

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
            {
                foreach (var bundle in value.Product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 != null && p1.ProductAttributeMappings.Any())
                    {
                        attributes2.AddRange(p1.ProductAttributeMappings);
                    }
                }
            }
            if (value.IgnoreNonCombinableAttributes)
            {
                attributes2 = attributes2.Where(x => !x.IsNonCombinable()).ToList();
            }
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
                    bool found = false;
                    //selected product attributes
                    foreach (var a1 in attributes1)
                    {
                        if (a1.Id == a2.Id)
                        {
                            var attributeValuesStr = ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, a1.Id);
                            foreach (string str1 in attributeValuesStr)
                            {
                                if (!String.IsNullOrEmpty(str1.Trim()))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    //if not found
                    if (!found)
                    {
                        var paa = await _productAttributeService.GetProductAttributeById(a2.ProductAttributeId);
                        if (paa != null)
                        {
                            var notFoundWarning = !string.IsNullOrEmpty(a2.TextPrompt) ?
                                a2.TextPrompt :
                                string.Format(_translationService.GetResource("ShoppingCart.SelectAttribute"), paa.Name);

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

                    var selectedReadOnlyValueIds = value.Product.ParseProductAttributeValues(value.ShoppingCartItem.Attributes)
                        //.Where(x => x.ProductAttributeMappingId == a2.Id)
                        .Select(x => x.Id)
                        .ToArray();

                    if (!CommonHelper.ArraysEqual(allowedReadOnlyValueIds, selectedReadOnlyValueIds))
                    {
                        warnings.Add("You cannot change read-only values");
                    }
                }
            }

            //validation rules
            foreach (var pam in attributes2)
            {
                if (!pam.ValidationRulesAllowed())
                    continue;

                //minimum length
                if (pam.ValidationMinLength.HasValue)
                {
                    if (pam.AttributeControlTypeId == AttributeControlType.TextBox ||
                        pam.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                    {
                        var valuesStr = ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, pam.Id);
                        var enteredText = valuesStr.FirstOrDefault();
                        int enteredTextLength = String.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                        if (pam.ValidationMinLength.Value > enteredTextLength)
                        {
                            var _pam = await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMinimumLength"), _pam.Name, pam.ValidationMinLength.Value));
                        }
                    }
                }

                //maximum length
                if (pam.ValidationMaxLength.HasValue)
                {
                    if (pam.AttributeControlTypeId == AttributeControlType.TextBox ||
                        pam.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                    {
                        var valuesStr = ProductExtensions.ParseValues(value.ShoppingCartItem.Attributes, pam.Id);
                        var enteredText = valuesStr.FirstOrDefault();
                        int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                        if (pam.ValidationMaxLength.Value < enteredTextLength)
                        {
                            var _pam = await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMaximumLength"), _pam.Name, pam.ValidationMaxLength.Value));
                        }
                    }
                }
            }
            return warnings;

        }


    }
}
