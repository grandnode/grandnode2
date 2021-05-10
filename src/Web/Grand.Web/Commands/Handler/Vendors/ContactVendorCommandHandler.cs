using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Common;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Models.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Vendors
{
    public class ContactVendorCommandHandler : IRequestHandler<ContactVendorSendCommand, ContactVendorModel>
    {
        private readonly IMessageProviderService _messageProviderService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;

        private readonly CommonSettings _commonSettings;

        public ContactVendorCommandHandler(IMessageProviderService messageProviderService, IWorkContext workContext,
            ITranslationService translationService, CommonSettings commonSettings)
        {
            _messageProviderService = messageProviderService;
            _workContext = workContext;
            _translationService = translationService;
            _commonSettings = commonSettings;
        }

        public async Task<ContactVendorModel> Handle(ContactVendorSendCommand request, CancellationToken cancellationToken)
        {
            var subject = _commonSettings.SubjectFieldOnContactUsForm ? request.Model.Subject : null;
            var body = FormatText.ConvertText(request.Model.Enquiry);

            await _messageProviderService.SendContactVendorMessage(_workContext.CurrentCustomer, request.Store, request.Vendor, _workContext.WorkingLanguage.Id,
                request.Model.Email.Trim(), request.Model.FullName, subject, body);

            request.Model.SuccessfullySent = true;
            request.Model.Result = _translationService.GetResource("ContactVendor.YourEnquiryHasBeenSent");
            return request.Model;
        }
    }
}
