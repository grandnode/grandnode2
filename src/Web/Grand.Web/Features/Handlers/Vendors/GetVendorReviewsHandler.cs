using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.Vendors;
using Grand.Web.Models.Vendors;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Vendors
{
    public class GetVendorReviewsHandler : IRequestHandler<GetVendorReviews, VendorReviewsModel>
    {
        private readonly IWorkContext _workContext;
        private readonly IVendorService _vendorService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IGroupService _groupService;
        private readonly CustomerSettings _customerSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly CaptchaSettings _captchaSettings;

        public GetVendorReviewsHandler(
            IWorkContext workContext, 
            IVendorService vendorService, 
            ICustomerService customerService, 
            IDateTimeService dateTimeService,
            IGroupService groupService,
            CustomerSettings customerSettings, 
            VendorSettings vendorSettings, 
            CaptchaSettings captchaSettings)
        {
            _workContext = workContext;
            _vendorService = vendorService;
            _customerService = customerService;
            _dateTimeService = dateTimeService;
            _groupService = groupService;
            _customerSettings = customerSettings;
            _vendorSettings = vendorSettings;
            _captchaSettings = captchaSettings;
        }

        public async Task<VendorReviewsModel> Handle(GetVendorReviews request, CancellationToken cancellationToken)
        {
            if (request.Vendor == null)
                throw new ArgumentNullException(nameof(request.Vendor));

            var model = new VendorReviewsModel();
            model.VendorId = request.Vendor.Id;
            model.VendorName = request.Vendor.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);
            model.VendorSeName = request.Vendor.GetSeName(_workContext.WorkingLanguage.Id);

            var vendorReviews = await _vendorService.GetAllVendorReviews("", true, null, null, "", request.Vendor.Id);
            foreach (var pr in vendorReviews)
            {
                var customer = await _customerService.GetCustomerById(pr.CustomerId);
                model.Items.Add(new VendorReviewModel
                {
                    Id = pr.Id,
                    CustomerId = pr.CustomerId,
                    CustomerName = customer.FormatUserName(_customerSettings.CustomerNameFormat),
                    Title = pr.Title,
                    ReviewText = pr.ReviewText,
                    Rating = pr.Rating,
                    Helpfulness = new VendorReviewHelpfulnessModel
                    {
                        VendorId = request.Vendor.Id,
                        VendorReviewId = pr.Id,
                        HelpfulYesTotal = pr.HelpfulYesTotal,
                        HelpfulNoTotal = pr.HelpfulNoTotal,
                    },
                    WrittenOnStr = _dateTimeService.ConvertToUserTime(pr.CreatedOnUtc, DateTimeKind.Utc).ToString("g"),
                });
            }

            model.AddVendorReview.CanCurrentCustomerLeaveReview = _vendorSettings.AllowAnonymousUsersToReviewVendor || !await _groupService.IsGuest(_workContext.CurrentCustomer);
            model.AddVendorReview.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnVendorReviewPage;

            return model;
        }
    }
}
