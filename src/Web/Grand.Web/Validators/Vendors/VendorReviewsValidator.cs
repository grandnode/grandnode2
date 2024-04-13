using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Vendors;

public class VendorReviewsValidator : BaseGrandValidator<VendorReviewsModel>
{
    public VendorReviewsValidator(
        IEnumerable<IValidatorConsumer<VendorReviewsModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IMediator mediator,
        IGroupService groupService, IWorkContext workContext, IVendorService vendorService,
        CaptchaSettings captchaSettings, VendorSettings vendorSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.AddVendorReview.Title).NotEmpty()
            .WithMessage(translationService.GetResource("Reviews.Fields.Title.Required"))
            .When(x => x.AddVendorReview != null);
        RuleFor(x => x.AddVendorReview.Title).Length(1, 200)
            .WithMessage(string.Format(translationService.GetResource("Reviews.Fields.Title.MaxLengthValidation"), 200))
            .When(x => x.AddVendorReview != null && !string.IsNullOrEmpty(x.AddVendorReview.Title));
        RuleFor(x => x.AddVendorReview.ReviewText).NotEmpty()
            .WithMessage(translationService.GetResource("Reviews.Fields.ReviewText.Required"))
            .When(x => x.AddVendorReview != null);
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var vendor = await vendorService.GetVendorById(x.VendorId);
            if (vendor is not { Active: true } || !vendor.AllowCustomerReviews)
                context.AddFailure(
                    translationService.GetResource("VendorReviews.VendorNotActiveOrAllowCustomerReviewsDisabled"));

            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !vendorSettings.AllowAnonymousUsersToReviewVendor)
                context.AddFailure(translationService.GetResource("VendorReviews.OnlyRegisteredUsersCanWriteReviews"));
            //allow reviews only by customer that bought something from this vendor
            if (vendorSettings.VendorReviewPossibleOnlyAfterPurchasing &&
                !(await mediator.Send(new GetOrderQuery {
                    CustomerId = workContext.CurrentCustomer.Id,
                    VendorId = x.VendorId,
                    Os = (int)OrderStatusSystem.Complete,
                    PageSize = 1
                }, _)).Any())
                context.AddFailure(
                    translationService.GetResource("VendorReviews.VendorReviewPossibleOnlyAfterPurchasing"));


            if (vendorSettings.VendorReviewPossibleOnlyOnce)
                if ((await vendorService.GetAllVendorReviews(
                        workContext.CurrentCustomer.Id,
                        null,
                        vendorId: vendor.Id,
                        pageSize: 1)).Any())
                    context.AddFailure(translationService.GetResource("VendorReviews.VendorReviewPossibleOnlyOnce"));
        });
        if (captchaSettings.Enabled && captchaSettings.ShowOnVendorReviewPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}