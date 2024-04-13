using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Catalog;

public class AddProductReviewValidator : BaseGrandValidator<AddProductReviewModel>
{
    public AddProductReviewValidator(
        IEnumerable<IValidatorConsumer<AddProductReviewModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage(translationService.GetResource("Reviews.Fields.Title.Required"));
        RuleFor(x => x.Title).Length(1, 200)
            .WithMessage(string.Format(translationService.GetResource("Reviews.Fields.Title.MaxLengthValidation"),
                200));
        RuleFor(x => x.ReviewText).NotEmpty()
            .WithMessage(translationService.GetResource("Reviews.Fields.ReviewText.Required"));
    }
}

public class ProductReviewsValidator : BaseGrandValidator<ProductReviewsModel>
{
    public ProductReviewsValidator(
        IEnumerable<IValidatorConsumer<ProductReviewsModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IGroupService groupService, IProductService productService,
        IOrderService orderService,
        IProductReviewService productReviewService,
        CaptchaSettings captchaSettings, CatalogSettings catalogSettings,
        IWorkContext workContext,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.AddProductReview.Title).NotEmpty()
            .WithMessage(translationService.GetResource("Reviews.Fields.Title.Required"));
        RuleFor(x => x.AddProductReview.Title).Length(1, 200).WithMessage(
            string.Format(translationService.GetResource("Reviews.Fields.Title.MaxLengthValidation"), 200));
        RuleFor(x => x.AddProductReview.ReviewText).NotEmpty()
            .WithMessage(translationService.GetResource("Reviews.Fields.ReviewText.Required"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnProductReviewPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var product = await productService.GetProductById(x.ProductId);
            if (product is not { Published: true } || !product.AllowCustomerReviews)
                context.AddFailure("Product is disabled");

            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !catalogSettings.AllowAnonymousUsersToReviewProduct)
                context.AddFailure(translationService.GetResource("Reviews.OnlyRegisteredUsersCanWriteReviews"));

            if (catalogSettings.ProductReviewPossibleOnlyAfterPurchasing &&
                !(await orderService.SearchOrders(customerId: workContext.CurrentCustomer.Id, productId: x.ProductId,
                    os: (int)OrderStatusSystem.Complete)).Any())
                context.AddFailure(translationService.GetResource("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));

            if (catalogSettings.ProductReviewPossibleOnlyOnce)
            {
                var reviews = await productReviewService.GetAllProductReviews(workContext.CurrentCustomer.Id,
                    productId: x.ProductId,
                    pageSize: 1);
                if (reviews.Any())
                    context.AddFailure(translationService.GetResource("Reviews.ProductReviewPossibleOnlyOnce"));
            }
        });
    }
}