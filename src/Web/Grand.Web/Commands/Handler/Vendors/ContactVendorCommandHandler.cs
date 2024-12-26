﻿using Grand.Business.Core.Interfaces.Common.Localization;
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
    private readonly IWorkContextAccessor _workContextAccessor;

    public ContactVendorCommandHandler(IMessageProviderService messageProviderService, IWorkContextAccessor workContextAccessor,
        ITranslationService translationService, CommonSettings commonSettings)
    {
        _messageProviderService = messageProviderService;
        _workContextAccessor = workContextAccessor;
        _translationService = translationService;
        _commonSettings = commonSettings;
    }

    public async Task<ContactVendorModel> Handle(ContactVendorSendCommand request, CancellationToken cancellationToken)
    {
        var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
        var body = FormatText.ConvertText(request.Model.Enquiry);

        await _messageProviderService.SendContactVendorMessage(_workContextAccessor.WorkContext.CurrentCustomer, request.Store,
            request.Vendor, _workContextAccessor.WorkContext.WorkingLanguage.Id,
            request.Model.Email.Trim(), request.Model.FullName, subject, body, request.IpAddress);

        request.Model.SuccessfullySent = true;
        request.Model.Result = _translationService.GetResource("ContactVendor.YourEnquiryHasBeenSent");
        return request.Model;
    }
}