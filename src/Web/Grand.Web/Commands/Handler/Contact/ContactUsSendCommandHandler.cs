using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Contact;
using Grand.Web.Models.Contact;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Contact;

public class ContactUsSendCommandHandler : IRequestHandler<ContactUsSendCommand, ContactUsModel>
{
    private readonly CommonSettings _commonSettings;
    private readonly IContactAttributeParser _contactAttributeParser;
    private readonly ICustomerMessageProviderService _customerMessageProviderService;
    private readonly ITranslationService _translationService;

    public ContactUsSendCommandHandler(
        IContactAttributeParser contactAttributeParser,
        ITranslationService translationService,
        ICustomerMessageProviderService customerMessageProviderService,
        CommonSettings commonSettings)
    {
        _contactAttributeParser = contactAttributeParser;
        _translationService = translationService;
        _customerMessageProviderService = customerMessageProviderService;
        _commonSettings = commonSettings;
    }

    public async Task<ContactUsModel> Handle(ContactUsSendCommand request, CancellationToken cancellationToken)
    {
        var attributes = await _contactAttributeParser.GetParseContactAttributes(request.Model.Attributes
                .Select(x => new CustomAttribute { Key = x.Key, Value = x.Value }).ToList(),
            CatalogAccess.For(request.Customer, request.Store.Id));

        request.Model.ContactAttribute = attributes;
        request.Model.ContactAttributeInfo =
            await _contactAttributeParser.FormatAttributes(request.Language, attributes,
                request.Store.LinkHost(request.Host));
        request.Model = await SendContactUs(request);

        return request.Model;
    }

    private async Task<ContactUsModel> SendContactUs(ContactUsSendCommand request)
    {
        var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
        var body = FormatText.ConvertText(request.Model.Enquiry);

        await _customerMessageProviderService.SendContactUsMessage
        (request.Customer, request.Store, request.Language.Id, request.Model.Email.Trim(),
            request.Model.FullName, subject,
            body, request.Model.ContactAttributeInfo, request.Model.ContactAttribute, request.IpAddress,
            host: request.Host);

        request.Model.SuccessfullySent = true;
        request.Model.Result = _translationService.GetResource("ContactUs.YourEnquiryHasBeenSent", request.Language.Id);

        return request.Model;
    }
}