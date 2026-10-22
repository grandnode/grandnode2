using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using ProductExtensions = Grand.Domain.Catalog.ProductExtensions;

namespace Grand.Web.AdminShared.Services;

public class ProductAttributeMappingViewModelService(
    IProductService productService,
    IPictureService pictureService,
    IProductAttributeService productAttributeService,
    ICurrencyService currencyService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    ILanguageService languageService,
    CurrencySettings currencySettings,
    IEnumTranslationService enumTranslationService)
    : IProductAttributeMappingViewModelService
{
    public virtual async Task PrepareProductAttributeValueModel(Product product,
        ProductModel.ProductAttributeValueModel model)
    {
        //pictures
        foreach (var x in product.ProductPictures.OrderBy(x => x.DisplayOrder))
            model.ProductPictureModels.Add(new ProductModel.ProductPictureModel {
                Id = x.Id,
                ProductId = product.Id,
                PictureId = x.PictureId,
                PictureUrl = await pictureService.GetPictureUrl(x.PictureId),
                DisplayOrder = x.DisplayOrder,
                IsDefault = x.IsDefault
            });

        var associatedProduct = await productService.GetProductById(model.AssociatedProductId);
        model.AssociatedProductName = associatedProduct != null ? associatedProduct.Name : "";
    }

    public virtual async Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        Product product)
    {
        var model = new ProductModel.ProductAttributeMappingModel {
            ProductId = product.Id
        };
        foreach (var attribute in await productAttributeService.GetAllProductAttributes(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            model.AvailableProductAttribute.Add(new SelectListItem {
                Value = attribute.Id,
                Text = attribute.Name
            });
        return model;
    }

    public virtual async Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        Product product, ProductAttributeMapping productAttributeMapping)
    {
        var model = productAttributeMapping.ToModel();
        foreach (var attribute in await productAttributeService.GetAllProductAttributes(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            model.AvailableProductAttribute.Add(new SelectListItem {
                Value = attribute.Id,
                Text = attribute.Name,
                Selected = attribute.Id == model.ProductAttributeId
            });
        return model;
    }

    public virtual async Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        ProductModel.ProductAttributeMappingModel model)
    {
        foreach (var attribute in await productAttributeService.GetAllProductAttributes(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            model.AvailableProductAttribute.Add(new SelectListItem {
                Value = attribute.Id,
                Text = attribute.Name
            });
        return model;
    }

    public virtual async Task<IList<ProductModel.ProductAttributeMappingModel>> PrepareProductAttributeMappingModels(
        Product product)
    {
        var items = new List<ProductModel.ProductAttributeMappingModel>();
        foreach (var x in product.ProductAttributeMappings.OrderBy(x => x.DisplayOrder))
        {
            var attributeModel = new ProductModel.ProductAttributeMappingModel {
                Id = x.Id,
                ProductId = product.Id,
                ProductAttribute = (await productAttributeService.GetProductAttributeById(x.ProductAttributeId))?.Name,
                ProductAttributeId = x.ProductAttributeId,
                TextPrompt = x.TextPrompt,
                IsRequired = x.IsRequired,
                ShowOnCatalogPage = x.ShowOnCatalogPage,
                AttributeControlType = enumTranslationService.GetTranslationEnum(x.AttributeControlTypeId),
                AttributeControlTypeId = x.AttributeControlTypeId,
                DisplayOrder = x.DisplayOrder,
                Combination = x.Combination
            };


            if (x.ShouldHaveValues())
            {
                attributeModel.ShouldHaveValues = true;
                attributeModel.TotalValues = x.ProductAttributeValues.Count;
            }

            if (x.ValidationRulesAllowed())
            {
                var validationRules = new StringBuilder(string.Empty);
                attributeModel.ValidationRulesAllowed = true;
                if (x.ValidationMinLength != null)
                    validationRules.AppendFormat("{0}: {1}<br />",
                        translationService.GetResource(
                            "Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MinLength"),
                        x.ValidationMinLength);
                if (x.ValidationMaxLength != null)
                    validationRules.AppendFormat("{0}: {1}<br />",
                        translationService.GetResource(
                            "Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MaxLength"),
                        x.ValidationMaxLength);
                if (!string.IsNullOrEmpty(x.ValidationFileAllowedExtensions))
                    validationRules.AppendFormat("{0}: {1}<br />",
                        translationService.GetResource(
                            "Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileAllowedExtensions"),
                        WebUtility.HtmlEncode(x.ValidationFileAllowedExtensions));
                if (x.ValidationFileMaximumSize != null)
                    validationRules.AppendFormat("{0}: {1}<br />",
                        translationService.GetResource(
                            "Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileMaximumSize"),
                        x.ValidationFileMaximumSize);
                if (!string.IsNullOrEmpty(x.DefaultValue))
                    validationRules.AppendFormat("{0}: {1}<br />",
                        translationService.GetResource(
                            "Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue"),
                        WebUtility.HtmlEncode(x.DefaultValue));
                attributeModel.ValidationRulesString = validationRules.ToString();
            }

            //currenty any attribute can have condition. why not?
            attributeModel.ConditionAllowed = true;
            var conditionAttribute = product.ParseProductAttributeMappings(x.ConditionAttribute).FirstOrDefault();
            var conditionValue = product.ParseProductAttributeValues(x.ConditionAttribute).FirstOrDefault();
            if (conditionAttribute != null && conditionValue != null)
            {
                var productAttribute =
                    await productAttributeService.GetProductAttributeById(conditionAttribute.ProductAttributeId);
                var _paname = productAttribute != null ? productAttribute.Name : "";
                attributeModel.ConditionString =
                    $"{WebUtility.HtmlEncode(_paname)}: {WebUtility.HtmlEncode(conditionValue.Name)}";
            }
            else
            {
                attributeModel.ConditionString = string.Empty;
            }

            items.Add(attributeModel);
        }

        return items;
    }

    public virtual async Task InsertProductAttributeMappingModel(ProductModel.ProductAttributeMappingModel model)
    {
        //insert mapping
        var productAttributeMapping = model.ToEntity();
        //predefined values
        var predefinedValues = (await productAttributeService.GetProductAttributeById(model.ProductAttributeId))
            .PredefinedProductAttributeValues;
        foreach (var predefinedValue in predefinedValues)
        {
            var pav = predefinedValue.ToEntity();
            //locales
            pav.Locales.Clear();
            var languages = await languageService.GetAllLanguages(true);
            //localization
            foreach (var lang in languages)
            {
                var name = predefinedValue.GetTranslation(x => x.Name, lang.Id, false);
                if (!string.IsNullOrEmpty(name))
                    pav.Locales.Add(new TranslationEntity { LanguageId = lang.Id, LocaleKey = "Name", LocaleValue = name });
            }

            productAttributeMapping.ProductAttributeValues.Add(pav);
        }

        await productAttributeService.InsertProductAttributeMapping(productAttributeMapping, model.ProductId);
    }

    public virtual async Task UpdateProductAttributeMappingModel(ProductModel.ProductAttributeMappingModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);
        if (product != null)
        {
            var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.Id);
            if (productAttributeMapping != null)
            {
                productAttributeMapping = model.ToEntity(productAttributeMapping);
                await productAttributeService.UpdateProductAttributeMapping(productAttributeMapping, model.ProductId);
            }
        }
    }

    public virtual async Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        ProductAttributeMapping productAttributeMapping)
    {
        var model = new ProductModel.ProductAttributeMappingModel {
            //prepare only used properties
            Id = productAttributeMapping.Id,
            ValidationRulesAllowed = productAttributeMapping.ValidationRulesAllowed(),
            AttributeControlTypeId = productAttributeMapping.AttributeControlTypeId,
            ValidationMinLength = productAttributeMapping.ValidationMinLength,
            ValidationMaxLength = productAttributeMapping.ValidationMaxLength,
            ValidationFileAllowedExtensions = productAttributeMapping.ValidationFileAllowedExtensions,
            ValidationFileMaximumSize = productAttributeMapping.ValidationFileMaximumSize,
            DefaultValue = productAttributeMapping.DefaultValue
        };
        return await Task.FromResult(model);
    }

    public virtual async Task UpdateProductAttributeValidationRulesModel(
        ProductAttributeMapping productAttributeMapping, ProductModel.ProductAttributeMappingModel model)
    {
        productAttributeMapping.ValidationMinLength = model.ValidationMinLength;
        productAttributeMapping.ValidationMaxLength = model.ValidationMaxLength;
        productAttributeMapping.ValidationFileAllowedExtensions = model.ValidationFileAllowedExtensions;
        productAttributeMapping.ValidationFileMaximumSize = model.ValidationFileMaximumSize;
        productAttributeMapping.DefaultValue = model.DefaultValue;
        await productAttributeService.UpdateProductAttributeMapping(productAttributeMapping, model.ProductId);
    }

    public virtual async Task<ProductAttributeConditionModel> PrepareProductAttributeConditionModel(Product product,
        ProductAttributeMapping productAttributeMapping)
    {
        var model = new ProductAttributeConditionModel {
            ProductAttributeMappingId = productAttributeMapping.Id,
            EnableCondition = productAttributeMapping.ConditionAttribute.Any(),
            ProductId = product.Id
        };
        //pre-select attribute and values
        var selectedPva = product.ParseProductAttributeMappings(productAttributeMapping.ConditionAttribute)
            .FirstOrDefault();

        var attributes = product.ProductAttributeMappings
            //ignore non-combinable attributes (should have selectable values)
            .Where(x => x.CanBeUsedAsCondition())
            //ignore this attribute (it cannot depend on itself)
            .Where(x => x.Id != productAttributeMapping.Id)
            .ToList();
        foreach (var attribute in attributes)
        {
            var pam = await productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
            var attributeModel = new ProductAttributeConditionModel.ProductAttributeModel {
                Id = attribute.Id,
                ProductAttributeId = attribute.ProductAttributeId,
                Name = pam.Name,
                TextPrompt = attribute.TextPrompt,
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlTypeId
            };

            if (attribute.ShouldHaveValues())
            {
                //values
                var attributeValues = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == attribute.Id)
                    ?.ProductAttributeValues;
                if (attributeValues != null)
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new ProductAttributeConditionModel.ProductAttributeValueModel {
                            Id = attributeValue.Id,
                            Name = attributeValue.Name,
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }

                //pre-select attribute and value
                if (selectedPva != null && attribute.Id == selectedPva.Id)
                {
                    //attribute
                    model.SelectedProductAttributeId = selectedPva.Id;

                    //values
                    switch (attribute.AttributeControlTypeId)
                    {
                        case AttributeControlType.DropdownList:
                        case AttributeControlType.RadioList:
                        case AttributeControlType.Checkboxes:
                        case AttributeControlType.ColorSquares:
                        case AttributeControlType.ImageSquares:
                            {
                                if (productAttributeMapping.ConditionAttribute.Any())
                                {
                                    //clear default selection
                                    foreach (var item in attributeModel.Values)
                                        item.IsPreSelected = false;

                                    //select new values
                                    var selectedValues =
                                        product.ParseProductAttributeValues(productAttributeMapping.ConditionAttribute);
                                    foreach (var attributeValue in selectedValues)
                                        foreach (var item in attributeModel.Values)
                                            if (attributeValue.Id == item.Id)
                                                item.IsPreSelected = true;
                                }
                            }
                            break;
                        case AttributeControlType.ReadonlyCheckboxes:
                        case AttributeControlType.TextBox:
                        case AttributeControlType.MultilineTextbox:
                        case AttributeControlType.Datepicker:
                        case AttributeControlType.FileUpload:
                        default:
                            //these attribute types are supported as conditions
                            break;
                    }
                }
            }

            model.ProductAttributes.Add(attributeModel);
        }

        return model;
    }

    public virtual async Task UpdateProductAttributeConditionModel(Product product,
        ProductAttributeMapping productAttributeMapping, ProductAttributeConditionModel model)
    {
        var customAttributes = new List<CustomAttribute>();
        if (model.EnableCondition)
        {
            var attribute =
                product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.SelectedProductAttributeId);
            if (attribute != null)
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, ctrlAttributes).ToList();
                            else
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, "").ToList();
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var cblAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                var anyValueSelected = false;
                                foreach (var item in cblAttributes.Split([','],
                                             StringSplitOptions.RemoveEmptyEntries))
                                    if (!string.IsNullOrEmpty(item))
                                    {
                                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                            attribute, item).ToList();
                                        anyValueSelected = true;
                                    }

                                if (!anyValueSelected)
                                    customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                        attribute, "").ToList();
                            }
                            else
                            {
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, "").ToList();
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.FileUpload:
                    default:
                        //these attribute types are supported as conditions
                        break;
                }
        }

        productAttributeMapping.ConditionAttribute = customAttributes;
        await productAttributeService.UpdateProductAttributeMapping(productAttributeMapping, model.ProductId);
    }

    public virtual async Task<ProductModel.ProductAttributeValueModel> PrepareProductAttributeValueModel(
        Product product, ProductAttributeMapping productAttributeMapping)
    {
        var model = new ProductModel.ProductAttributeValueModel {
            ProductAttributeMappingId = productAttributeMapping.Id,
            ProductId = product.Id,

            //color squares
            DisplayColorSquaresRgb =
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.ColorSquares,
            ColorSquaresRgb = "#000000",
            //image squares
            DisplayImageSquaresPicture =
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.ImageSquares,
            PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))
                ?.CurrencyCode,
            //default qantity for associated product
            Quantity = 1
        };

        //pictures
        foreach (var x in product.ProductPictures)
            model.ProductPictureModels.Add(new ProductModel.ProductPictureModel {
                Id = x.Id,
                ProductId = product.Id,
                PictureId = x.PictureId,
                PictureUrl = await pictureService.GetPictureUrl(x.PictureId),
                DisplayOrder = x.DisplayOrder,
                IsDefault = x.IsDefault
            });
        return model;
    }

    public virtual async Task<IList<ProductModel.ProductAttributeValueModel>> PrepareProductAttributeValueModels(
        Product product, ProductAttributeMapping productAttributeMapping)
    {
        var items = new List<ProductModel.ProductAttributeValueModel>();
        foreach (var x in productAttributeMapping.ProductAttributeValues.OrderBy(x => x.DisplayOrder))
        {
            Product associatedProduct = null;
            if (x.AttributeValueTypeId == AttributeValueType.AssociatedToProduct)
                associatedProduct = await productService.GetProductById(x.AssociatedProductId);

            var pictureThumbnailUrl =
                await pictureService.GetPictureUrl(
                    string.IsNullOrEmpty(x.PictureId) ? x.ImageSquaresPictureId : x.PictureId, 100, false);

            if (string.IsNullOrEmpty(pictureThumbnailUrl))
                pictureThumbnailUrl = await pictureService.GetPictureUrl("", 1);

            items.Add(new ProductModel.ProductAttributeValueModel {
                Id = x.Id,
                ProductAttributeMappingId = productAttributeMapping.Id, //TODO - check x.ProductAttributeMappingId,
                AttributeValueTypeId = x.AttributeValueTypeId,
                AttributeValueTypeName = enumTranslationService.GetTranslationEnum(x.AttributeValueTypeId),
                AssociatedProductId = x.AssociatedProductId,
                AssociatedProductName = associatedProduct != null ? associatedProduct.Name : "",
                Name = productAttributeMapping.AttributeControlTypeId != AttributeControlType.ColorSquares
                    ? x.Name
                    : $"{x.Name} - {x.ColorSquaresRgb}",
                ColorSquaresRgb = x.ColorSquaresRgb,
                ImageSquaresPictureId = x.ImageSquaresPictureId,
                PriceAdjustment = x.PriceAdjustment,
                PriceAdjustmentStr = x.PriceAdjustment.ToString("G29"),
                WeightAdjustment = x.WeightAdjustment,
                WeightAdjustmentStr = x.AttributeValueTypeId == AttributeValueType.Simple
                    ? x.WeightAdjustment.ToString("G29")
                    : "",
                Cost = x.Cost,
                PrimaryStoreCurrencyCode =
                    (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,
                Quantity = x.Quantity,
                IsPreSelected = x.IsPreSelected,
                DisplayOrder = x.DisplayOrder,
                PictureId = x.PictureId,
                PictureThumbnailUrl = pictureThumbnailUrl,
                ProductId = product.Id
            });
        }

        return items;
    }

    public virtual async Task<ProductModel.ProductAttributeValueModel> PrepareProductAttributeValueModel(
        ProductAttributeMapping pa, ProductAttributeValue pav)
    {
        var associatedProduct = await productService.GetProductById(pav.AssociatedProductId);

        var model = new ProductModel.ProductAttributeValueModel {
            ProductAttributeMappingId = pa.Id, //TODO - check pav.ProductAttributeMappingId,
            AttributeValueTypeId = pav.AttributeValueTypeId,
            AttributeValueTypeName = enumTranslationService.GetTranslationEnum(pav.AttributeValueTypeId),
            AssociatedProductId = pav.AssociatedProductId,
            AssociatedProductName = associatedProduct != null ? associatedProduct.Name : "",
            Name = pav.Name,
            ColorSquaresRgb = pav.ColorSquaresRgb,
            DisplayColorSquaresRgb = pa.AttributeControlTypeId == AttributeControlType.ColorSquares,
            ImageSquaresPictureId = pav.ImageSquaresPictureId,
            DisplayImageSquaresPicture = pa.AttributeControlTypeId == AttributeControlType.ImageSquares,
            PriceAdjustment = pav.PriceAdjustment,
            WeightAdjustment = pav.WeightAdjustment,
            Cost = pav.Cost,
            PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))
                ?.CurrencyCode,
            Quantity = pav.Quantity,
            IsPreSelected = pav.IsPreSelected,
            DisplayOrder = pav.DisplayOrder,
            PictureId = pav.PictureId
        };
        if (model.DisplayColorSquaresRgb && string.IsNullOrEmpty(model.ColorSquaresRgb))
            model.ColorSquaresRgb = "#000000";
        return model;
    }

    public virtual async Task InsertProductAttributeValueModel(ProductModel.ProductAttributeValueModel model)
    {
        var pav = new ProductAttributeValue {
            AttributeValueTypeId = model.AttributeValueTypeId,
            AssociatedProductId = model.AssociatedProductId,
            Name = model.Name,
            ColorSquaresRgb = model.ColorSquaresRgb,
            ImageSquaresPictureId = model.ImageSquaresPictureId,
            PriceAdjustment = model.PriceAdjustment,
            WeightAdjustment = model.WeightAdjustment,
            Cost = model.Cost,
            Quantity = model.Quantity,
            IsPreSelected = model.IsPreSelected,
            DisplayOrder = model.DisplayOrder,
            PictureId = model.PictureId,
            Locales = model.Locales.ToTranslationProperty()
        };
        await productAttributeService.InsertProductAttributeValue(pav, model.ProductId,
            model.ProductAttributeMappingId);
    }

    public virtual async Task UpdateProductAttributeValueModel(ProductAttributeValue pav,
        ProductModel.ProductAttributeValueModel model)
    {
        pav.AttributeValueTypeId = model.AttributeValueTypeId;
        pav.AssociatedProductId = model.AssociatedProductId;
        pav.Name = model.Name;
        pav.ColorSquaresRgb = model.ColorSquaresRgb;
        pav.ImageSquaresPictureId = model.ImageSquaresPictureId;
        pav.PriceAdjustment = model.PriceAdjustment;
        pav.WeightAdjustment = model.WeightAdjustment;
        pav.Cost = model.Cost;
        pav.Quantity = model.Quantity;
        pav.IsPreSelected = model.IsPreSelected;
        pav.DisplayOrder = model.DisplayOrder;
        pav.PictureId = model.PictureId;
        pav.Locales = model.Locales.ToTranslationProperty();

        await productAttributeService.UpdateProductAttributeValue(pav, model.ProductId,
            model.ProductAttributeMappingId);
    }
}
