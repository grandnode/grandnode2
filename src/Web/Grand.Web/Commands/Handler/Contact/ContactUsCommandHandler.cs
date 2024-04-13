using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Web.Commands.Models.Contact;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Handler.Contact;

public class ContactUsCommandHandler : IRequestHandler<ContactUsCommand, ContactUsModel>
{
    private readonly CaptchaSettings _captchaSettings;

    private readonly CommonSettings _commonSettings;

    private readonly IContactAttributeService _contactAttributeService;

    public ContactUsCommandHandler(IContactAttributeService contactAttributeService, CommonSettings commonSettings,
        CaptchaSettings captchaSettings)
    {
        _contactAttributeService = contactAttributeService;
        _commonSettings = commonSettings;
        _captchaSettings = captchaSettings;
    }

    public async Task<ContactUsModel> Handle(ContactUsCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? new ContactUsModel {
            Email = request.Customer.Email,
            FullName = request.Customer.GetFullName()
        };

        model.SubjectEnabled = _commonSettings.SubjectFieldOnContactUsForm;
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage;

        model.ContactAttributes = await PrepareContactAttributeModel(request);

        return model;
    }

    private async Task<IList<ContactUsModel.ContactAttributeModel>> PrepareContactAttributeModel(
        ContactUsCommand request)
    {
        var model = new List<ContactUsModel.ContactAttributeModel>();

        var contactAttributes = await _contactAttributeService.GetAllContactAttributes(request.Store.Id);
        foreach (var attribute in contactAttributes)
        {
            var attributeModel = new ContactUsModel.ContactAttributeModel {
                Id = attribute.Id,
                Name = attribute.GetTranslation(x => x.Name, request.Language.Id),
                TextPrompt = attribute.GetTranslation(x => x.TextPrompt, request.Language.Id),
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlType,
                DefaultValue = request.Model?.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value ??
                               attribute.DefaultValue
            };
            if (attribute.AttributeControlType == AttributeControlType.Datepicker)
            {
                int.TryParse(request.Model?.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_day")?.Value,
                    out var selectedDay);
                int.TryParse(request.Model?.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_month")?.Value,
                    out var selectedMonth);
                int.TryParse(request.Model?.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_year")?.Value,
                    out var selectedYear);

                if (selectedDay > 0)
                    attributeModel.SelectedDay = selectedDay;

                if (selectedMonth > 0)
                    attributeModel.SelectedMonth = selectedMonth;

                if (selectedYear > 0)
                    attributeModel.SelectedYear = selectedYear;
            }

            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            if (attribute.ShouldHaveValues())
            {
                //values
                var attributeValues = attribute.ContactAttributeValues;
                foreach (var attributeValue in attributeValues)
                {
                    var preSelected = request.Model?.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;

                    var attributeValueModel = new ContactUsModel.ContactAttributeValueModel {
                        Id = attributeValue.Id,
                        Name = attributeValue.GetTranslation(x => x.Name, request.Language.Id),
                        ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                        IsPreSelected = string.IsNullOrEmpty(preSelected)
                            ? attributeValue.IsPreSelected
                            : preSelected.Contains(attributeValue.Id),
                        DisplayOrder = attributeValue.DisplayOrder
                    };
                    attributeModel.Values.Add(attributeValueModel);
                }
            }

            model.Add(attributeModel);
        }

        return model;
    }
}