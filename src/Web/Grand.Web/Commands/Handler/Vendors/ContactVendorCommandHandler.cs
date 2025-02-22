using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Models.Vendors;
using MediatR;

namespace Grand.Web.Commands.Handler.Vendors;

public class ContactVendorCommandHandler : IRequestHandler<ContactVendorSendCommand, ContactVendorModel>
{
    private readonly CommonSettings _commonSettings;
    private readonly IMessageProviderService _messageProviderService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public ContactVendorCommandHandler(IMessageProviderService messageProviderService, IContextAccessor contextAccessor,
        ITranslationService translationService, CommonSettings commonSettings)
    {
        _messageProviderService = messageProviderService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _commonSettings = commonSettings;
    }

    public async Task<ContactVendorModel> Handle(ContactVendorSendCommand request, CancellationToken cancellationToken)
    {
        var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
        var body = FormatText.ConvertText(request.Model.Enquiry);

        await _messageProviderService.SendContactVendorMessage(_contextAccessor.WorkContext.CurrentCustomer, request.Store,
            request.Vendor, _contextAccessor.WorkContext.WorkingLanguage.Id,
            request.Model.Email.Trim(), request.Model.FullName, subject, body, request.IpAddress);

        request.Model.SuccessfullySent = true;
        request.Model.Result = _translationService.GetResource("ContactVendor.YourEnquiryHasBeenSent");
        return request.Model;
    }
}