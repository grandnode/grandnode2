using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Catalog;

public class ProductAskQuestionSimpleValidator : BaseGrandValidator<ProductAskQuestionSimpleModel>
{
    public ProductAskQuestionSimpleValidator(
        IEnumerable<IValidatorConsumer<ProductAskQuestionSimpleModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        CaptchaSettings captchaSettings, CatalogSettings catalogSettings,
        IProductService productService,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.AskQuestionEmail).NotEmpty()
            .WithMessage(translationService.GetResource("Products.AskQuestion.Email.Required"));
        RuleFor(x => x.AskQuestionEmail).EmailAddress()
            .WithMessage(translationService.GetResource("Common.WrongEmail"));
        RuleFor(x => x.AskQuestionMessage).NotEmpty()
            .WithMessage(translationService.GetResource("Products.AskQuestion.Message.Required"));
        RuleFor(x => x.AskQuestionFullName).NotEmpty()
            .WithMessage(translationService.GetResource("Products.AskQuestion.FullName.Required"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnAskQuestionPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var product = await productService.GetProductById(x.Id);
            if (product is not { Published: true } || !catalogSettings.AskQuestionOnProduct)
                context.AddFailure("Product is disabled");
        });
    }
}