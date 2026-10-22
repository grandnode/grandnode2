using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Models.Vendors;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Commands.Handler.Vendors;

public class ContactVendorCommandHandler : IRequestHandler<ContactVendorSendCommand, ContactVendorModel>
{
    private readonly CommonSettings _commonSettings;
    private readonly IVendorMessageProviderService _vendorMessageProviderService;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public ContactVendorCommandHandler(IVendorMessageProviderService vendorMessageProviderService, IContextAccessor contextAccessor,
        IRequestTranslationService translationService, CommonSettings commonSettings)
    {
        _vendorMessageProviderService = vendorMessageProviderService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _commonSettings = commonSettings;
    }

    public async Task<ContactVendorModel> Handle(ContactVendorSendCommand request, CancellationToken cancellationToken)
    {
        var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
        var body = FormatText.ConvertText(request.Model.Enquiry);

        await _vendorMessageProviderService.SendContactVendorMessage(_contextAccessor.WorkContext.CurrentCustomer, request.Store,
            request.Vendor, _contextAccessor.WorkContext.WorkingLanguage.Id,
            request.Model.Email.Trim(), request.Model.FullName, subject, body, request.IpAddress,
            host: _contextAccessor.StoreContext.CurrentHost);

        request.Model.SuccessfullySent = true;
        request.Model.Result = _translationService.GetResource("ContactVendor.YourEnquiryHasBeenSent");
        return request.Model;
    }
}