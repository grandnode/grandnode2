using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class ContactAttributeViewModelService(
    IContactAttributeService contactAttributeService,
    IContactAttributeParser contactAttributeParser,
    ITranslationService translationService,
    IWorkContext workContext)
    : IContactAttributeViewModelService
{
    public virtual async Task<IEnumerable<ContactAttributeModel>> PrepareContactAttributeListModel()
    {
        var contactAttributes =
            await contactAttributeService.GetAllContactAttributes(workContext.CurrentCustomer.StaffStoreId, true);
        return contactAttributes.Select(x =>
        {
            var attributeModel = x.ToModel();
            attributeModel.AttributeControlTypeName =
                x.AttributeControlType.GetTranslationEnum(translationService, workContext);
            return attributeModel;
        });
    }

    public virtual async Task PrepareConditionAttributes(ContactAttributeModel model, ContactAttribute contactAttribute)
    {
        ArgumentNullException.ThrowIfNull(model);

        //currenty any contact attribute can have condition.
        model.ConditionAllowed = true;

        if (contactAttribute == null)
            return;

        var selectedAttribute =
            (await contactAttributeParser.ParseContactAttributes(contactAttribute.ConditionAttribute)).FirstOrDefault();
        var selectedValues =
            await contactAttributeParser.ParseContactAttributeValues(contactAttribute.ConditionAttribute);

        model.ConditionModel = new ConditionModel {
            EnableCondition = contactAttribute.ConditionAttribute.Any(),
            SelectedAttributeId = selectedAttribute != null ? selectedAttribute.Id : "",
            ConditionAttributes =
                (await contactAttributeService.GetAllContactAttributes(workContext.CurrentCustomer.StaffStoreId, true))
                //ignore this attribute and non-combinable attributes
                .Where(x => x.Id != contactAttribute.Id && x.CanBeUsedAsCondition())
                .Select(x =>
                    new AttributeConditionModel {
                        Id = x.Id,
                        Name = x.Name,
                        AttributeControlType = x.AttributeControlType,
                        Values = x.ContactAttributeValues
                            .Select(v => new SelectListItem {
                                Text = v.Name,
                                Value = v.Id.ToString(),
                                Selected = selectedAttribute != null && selectedAttribute.Id == x.Id &&
                                           selectedValues.Any(sv => sv.Id == v.Id)
                            }).ToList()
                    }).ToList()
        };
    }

    public virtual async Task<ContactAttribute> InsertContactAttributeModel(ContactAttributeModel model)
    {
        var contactAttribute = model.ToEntity();
        await contactAttributeService.InsertContactAttribute(contactAttribute);
        return contactAttribute;
    }

    public virtual async Task<ContactAttribute> UpdateContactAttributeModel(ContactAttribute contactAttribute,
        ContactAttributeModel model)
    {
        contactAttribute = model.ToEntity(contactAttribute);
        await SaveConditionAttributes(contactAttribute, model);
        await contactAttributeService.UpdateContactAttribute(contactAttribute);
        return contactAttribute;
    }

    public virtual ContactAttributeValueModel PrepareContactAttributeValueModel(ContactAttribute contactAttribute)
    {
        var model = new ContactAttributeValueModel {
            ContactAttributeId = contactAttribute.Id,
            //color squares
            DisplayColorSquaresRgb = contactAttribute.AttributeControlType == AttributeControlType.ColorSquares,
            ColorSquaresRgb = "#000000"
        };

        return model;
    }

    public virtual ContactAttributeValueModel PrepareContactAttributeValueModel(ContactAttribute contactAttribute,
        ContactAttributeValue contactAttributeValue)
    {
        var model = contactAttributeValue.ToModel();
        model.DisplayColorSquaresRgb = contactAttribute.AttributeControlType == AttributeControlType.ColorSquares;
        return model;
    }

    public virtual async Task<ContactAttributeValue> InsertContactAttributeValueModel(ContactAttribute contactAttribute,
        ContactAttributeValueModel model)
    {
        var cav = model.ToEntity();
        contactAttribute.ContactAttributeValues.Add(cav);
        await contactAttributeService.UpdateContactAttribute(contactAttribute);
        return cav;
    }

    public virtual async Task<ContactAttributeValue> UpdateContactAttributeValueModel(ContactAttribute contactAttribute,
        ContactAttributeValue contactAttributeValue, ContactAttributeValueModel model)
    {
        contactAttributeValue = model.ToEntity(contactAttributeValue);
        await contactAttributeService.UpdateContactAttribute(contactAttribute);
        return contactAttributeValue;
    }

    #region Utilities

    protected virtual async Task SaveConditionAttributes(ContactAttribute contactAttribute, ContactAttributeModel model)
    {
        var customattributes = new List<CustomAttribute>();
        if (model.ConditionModel.EnableCondition)
        {
            var attribute =
                await contactAttributeService.GetContactAttributeById(model.ConditionModel.SelectedAttributeId);
            if (attribute != null)
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    {
                        var selectedAttribute = model.ConditionModel.ConditionAttributes
                            .FirstOrDefault(x => x.Id == model.ConditionModel.SelectedAttributeId);
                        var selectedValue = selectedAttribute?.SelectedValueId;
                        customattributes = !string.IsNullOrEmpty(selectedValue)
                            ? contactAttributeParser.AddContactAttribute(customattributes, attribute, selectedValue)
                                .ToList()
                            : contactAttributeParser.AddContactAttribute(customattributes, attribute, string.Empty)
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
                                customattributes = contactAttributeParser
                                    .AddContactAttribute(customattributes, attribute, value).ToList();
                        else
                            customattributes = contactAttributeParser
                                .AddContactAttribute(customattributes, attribute, string.Empty).ToList();
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

        contactAttribute.ConditionAttribute = customattributes;
    }

    #endregion
}