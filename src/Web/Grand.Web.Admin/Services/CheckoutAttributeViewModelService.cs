using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CheckoutAttributeViewModelService(
    ICheckoutAttributeService checkoutAttributeService,
    ICheckoutAttributeParser checkoutAttributeParser,
    ITranslationService translationService,
    ITaxCategoryService taxCategoryService,
    IWorkContext workContext,
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    IMeasureService measureService,
    MeasureSettings measureSettings)
    : ICheckoutAttributeViewModelService
{
    public virtual async Task<IEnumerable<CheckoutAttributeModel>> PrepareCheckoutAttributeListModel()
    {
        var checkoutAttributes = await checkoutAttributeService.GetAllCheckoutAttributes(ignoreAcl: true);
        return checkoutAttributes.Select((Func<CheckoutAttribute, CheckoutAttributeModel>)(x =>
        {
            var attributeModel = x.ToModel();
            attributeModel.AttributeControlTypeName =
                x.AttributeControlTypeId.GetTranslationEnum(translationService, workContext);
            return attributeModel;
        }));
    }

    public virtual async Task<IEnumerable<CheckoutAttributeValueModel>> PrepareCheckoutAttributeValuesModel(
        string checkoutAttributeId)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        var values = checkoutAttribute.CheckoutAttributeValues;
        return values.Select(x => new CheckoutAttributeValueModel {
            Id = x.Id,
            CheckoutAttributeId = x.CheckoutAttributeId,
            Name = checkoutAttribute.AttributeControlTypeId != AttributeControlType.ColorSquares
                ? x.Name
                : $"{x.Name} - {x.ColorSquaresRgb}",
            ColorSquaresRgb = x.ColorSquaresRgb,
            PriceAdjustment = x.PriceAdjustment,
            WeightAdjustment = x.WeightAdjustment,
            IsPreSelected = x.IsPreSelected,
            DisplayOrder = x.DisplayOrder
        });
    }

    public virtual async Task<CheckoutAttributeModel> PrepareCheckoutAttributeModel()
    {
        var model = new CheckoutAttributeModel();
        //tax categories
        await PrepareTaxCategories(model, null, true);
        //condition
        await PrepareConditionAttributes(model, null);
        return model;
    }

    public virtual async Task<CheckoutAttributeValueModel> PrepareCheckoutAttributeValueModel(
        string checkoutAttributeId)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        var model = new CheckoutAttributeValueModel {
            CheckoutAttributeId = checkoutAttributeId,
            PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))
                .CurrencyCode,
            BaseWeightIn = (await measureService.GetMeasureWeightById(measureSettings.BaseWeightId)).Name,
            //color squares
            DisplayColorSquaresRgb = checkoutAttribute.AttributeControlTypeId == AttributeControlType.ColorSquares,
            ColorSquaresRgb = "#000000"
        };

        return model;
    }

    public virtual async Task<CheckoutAttributeValueModel> PrepareCheckoutAttributeValueModel(
        CheckoutAttribute checkoutAttribute, CheckoutAttributeValue checkoutAttributeValue)
    {
        var model = checkoutAttributeValue.ToModel();
        model.DisplayColorSquaresRgb = checkoutAttribute.AttributeControlTypeId == AttributeControlType.ColorSquares;
        model.PrimaryStoreCurrencyCode =
            (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
        model.BaseWeightIn = (await measureService.GetMeasureWeightById(measureSettings.BaseWeightId)).Name;

        return model;
    }

    public virtual async Task<CheckoutAttribute> InsertCheckoutAttributeModel(CheckoutAttributeModel model)
    {
        var checkoutAttribute = model.ToEntity();
        await checkoutAttributeService.InsertCheckoutAttribute(checkoutAttribute);

        return checkoutAttribute;
    }

    public virtual async Task<CheckoutAttribute> UpdateCheckoutAttributeModel(CheckoutAttribute checkoutAttribute,
        CheckoutAttributeModel model)
    {
        checkoutAttribute = model.ToEntity(checkoutAttribute);
        await SaveConditionAttributes(checkoutAttribute, model);
        await checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
        return checkoutAttribute;
    }

    public virtual async Task<CheckoutAttributeValue> InsertCheckoutAttributeValueModel(
        CheckoutAttribute checkoutAttribute, CheckoutAttributeValueModel model)
    {
        var cav = model.ToEntity();
        checkoutAttribute.CheckoutAttributeValues.Add(cav);
        await checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
        return cav;
    }

    public virtual async Task<CheckoutAttributeValue> UpdateCheckoutAttributeValueModel(
        CheckoutAttribute checkoutAttribute, CheckoutAttributeValue checkoutAttributeValue,
        CheckoutAttributeValueModel model)
    {
        checkoutAttributeValue = model.ToEntity(checkoutAttributeValue);
        await checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
        return checkoutAttributeValue;
    }

    #region Utilities

    public virtual async Task PrepareTaxCategories(CheckoutAttributeModel model, CheckoutAttribute checkoutAttribute,
        bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);

        //tax categories
        var taxCategories = await taxCategoryService.GetAllTaxCategories();
        model.AvailableTaxCategories.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"), Value = ""
        });
        foreach (var tc in taxCategories)
            model.AvailableTaxCategories.Add(new SelectListItem {
                Text = tc.Name, Value = tc.Id,
                Selected = checkoutAttribute != null && !excludeProperties && tc.Id == checkoutAttribute.TaxCategoryId
            });
    }

    public virtual async Task PrepareConditionAttributes(CheckoutAttributeModel model,
        CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(model);

        //currenty any checkout attribute can have condition.
        model.ConditionAllowed = true;

        if (checkoutAttribute == null)
            return;

        var selectedAttribute =
            (await checkoutAttributeParser.ParseCheckoutAttributes(checkoutAttribute.ConditionAttribute))
            .FirstOrDefault();
        var selectedValues =
            await checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttribute.ConditionAttribute);

        model.ConditionModel = new ConditionModel {
            EnableCondition = checkoutAttribute.ConditionAttribute.Any(),
            SelectedAttributeId = selectedAttribute != null ? selectedAttribute.Id : "",
            ConditionAttributes = (await checkoutAttributeService.GetAllCheckoutAttributes(ignoreAcl: false))
                //ignore this attribute and non-combinable attributes
                .Where(x => x.Id != checkoutAttribute.Id && x.CanBeUsedAsCondition())
                .Select(x =>
                    new AttributeConditionModel {
                        Id = x.Id,
                        Name = x.Name,
                        AttributeControlType = x.AttributeControlTypeId,
                        Values = x.CheckoutAttributeValues
                            .Select(v => new SelectListItem {
                                Text = v.Name,
                                Value = v.Id.ToString(),
                                Selected = selectedAttribute != null && selectedAttribute.Id == x.Id &&
                                           selectedValues.Any(sv => sv.Id == v.Id)
                            }).ToList()
                    }).ToList()
        };
    }

    protected virtual async Task SaveConditionAttributes(CheckoutAttribute checkoutAttribute,
        CheckoutAttributeModel model)
    {
        var conditionAttributes = new List<CustomAttribute>();
        if (model.ConditionModel.EnableCondition)
        {
            var attribute =
                await checkoutAttributeService.GetCheckoutAttributeById(model.ConditionModel.SelectedAttributeId);
            if (attribute != null)
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    {
                        var selectedAttribute = model.ConditionModel.ConditionAttributes
                            .FirstOrDefault(x => x.Id == model.ConditionModel.SelectedAttributeId);
                        var selectedValue = selectedAttribute?.SelectedValueId;
                        conditionAttributes = !string.IsNullOrEmpty(selectedValue)
                            ? checkoutAttributeParser
                                .AddCheckoutAttribute(conditionAttributes, attribute, selectedValue).ToList()
                            : checkoutAttributeParser.AddCheckoutAttribute(conditionAttributes, attribute, string.Empty)
                                .ToList();
                    }
                        break;
                    case AttributeControlType.Checkboxes:
                    {
                        var selectedAttribute = model.ConditionModel.ConditionAttributes
                            .FirstOrDefault(x => x.Id == model.ConditionModel.SelectedAttributeId);
                        var selectedValues = selectedAttribute?.Values.Where(x => x.Selected).Select(x => x.Value);
                        if (selectedValues.Any())
                            foreach (var value in selectedValues)
                                conditionAttributes = checkoutAttributeParser
                                    .AddCheckoutAttribute(conditionAttributes, attribute, value).ToList();
                        else
                            conditionAttributes = checkoutAttributeParser
                                .AddCheckoutAttribute(conditionAttributes, attribute, string.Empty).ToList();
                    }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.FileUpload:
                    default:
                        //these attribute types are not supported as conditions
                        break;
                }
        }

        checkoutAttribute.ConditionAttribute = conditionAttributes;
    }

    #endregion
}