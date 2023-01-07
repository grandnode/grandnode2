﻿using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Vendors;
using MediatR;

namespace Grand.Web.Commands.Handler.Vendors
{
    public class InsertVendorReviewCommandHandler : IRequestHandler<InsertVendorReviewCommand, VendorReview>
    {
        private readonly IVendorService _vendorService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly IMessageProviderService _messageProviderService;

        private readonly LanguageSettings _languageSettings;
        private readonly VendorSettings _vendorSettings;


        public InsertVendorReviewCommandHandler(IVendorService vendorService, IWorkContext workContext,
            ICustomerService customerService, IMessageProviderService messageProviderService,
            LanguageSettings languageSettings, VendorSettings vendorSettings)
        {
            _vendorService = vendorService;
            _workContext = workContext;
            _customerService = customerService;
            _messageProviderService = messageProviderService;
            _languageSettings = languageSettings;
            _vendorSettings = vendorSettings;
        }

        public async Task<VendorReview> Handle(InsertVendorReviewCommand request, CancellationToken cancellationToken)
        {
            //save review
            var rating = request.Model.AddVendorReview.Rating;
            if (rating is < 1 or > 5)
                rating = _vendorSettings.DefaultVendorRatingValue;
            var isApproved = !_vendorSettings.VendorReviewsMustBeApproved;

            var vendorReview = new VendorReview
            {
                VendorId = request.Vendor.Id,
                CustomerId = _workContext.CurrentCustomer.Id,
                Title = request.Model.AddVendorReview.Title,
                ReviewText = request.Model.AddVendorReview.ReviewText,
                Rating = rating,
                HelpfulYesTotal = 0,
                HelpfulNoTotal = 0,
                IsApproved = isApproved,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _vendorService.InsertVendorReview(vendorReview);

            if (!_workContext.CurrentCustomer.HasContributions)
            {
                await _customerService.UpdateContributions(_workContext.CurrentCustomer);
            }

            //update vendor totals
            await _vendorService.UpdateVendorReviewTotals(request.Vendor);

            //notify store owner
            if (_vendorSettings.NotifyVendorAboutNewVendorReviews)
                await _messageProviderService.SendVendorReviewMessage(vendorReview, request.Store, _languageSettings.DefaultAdminLanguageId);

            return vendorReview;
        }
    }
}
