using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Contact;
using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Handler.Contact;

public class ContactUsSendCommandHandler : IRequestHandler<ContactUsSendCommand, ContactUsModel>
{
    private readonly CommonSettings _commonSettings;
    private readonly IContactAttributeParser _contactAttributeParser;
    private readonly IMessageProviderService _messageProviderService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public ContactUsSendCommandHandler(IWorkContext workContext,
        IContactAttributeParser contactAttributeParser,
        ITranslationService translationService,
        IMessageProviderService messageProviderService,
        CommonSettings commonSettings)
    {
        _workContext = workContext;
        _contactAttributeParser = contactAttributeParser;
        _translationService = translationService;
        _messageProviderService = messageProviderService;
        _commonSettings = commonSettings;
    }

    public async Task<ContactUsModel> Handle(ContactUsSendCommand request, CancellationToken cancellationToken)
    {
        var attributes = await _contactAttributeParser.GetParseContactAttributes(request.Model.Attributes
            .Select(x => new CustomAttribute { Key = x.Key, Value = x.Value }).ToList());

        request.Model.ContactAttribute = attributes;
        request.Model.ContactAttributeInfo =
            await _contactAttributeParser.FormatAttributes(_workContext.WorkingLanguage, attributes,
                _workContext.CurrentCustomer);
        request.Model = await SendContactUs(request, _workContext.CurrentStore);

        return request.Model;
    }

    private async Task<ContactUsModel> SendContactUs(ContactUsSendCommand request, Store store)
    {
        var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
        var body = FormatText.ConvertText(request.Model.Enquiry);

        await _messageProviderService.SendContactUsMessage
        (_workContext.CurrentCustomer, store, _workContext.WorkingLanguage.Id, request.Model.Email.Trim(),
            request.Model.FullName, subject,
            body, request.Model.ContactAttributeInfo, request.Model.ContactAttribute, request.IpAddress);

        request.Model.SuccessfullySent = true;
        request.Model.Result = _translationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

        return request.Model;
    }
}