using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Queries.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Affiliates;
using MediatR;

namespace Grand.Web.Admin.Validators.Affiliate;

public class AffiliateDeleteValidator : BaseGrandValidator<AffiliateDeleteModel>
{
    public AffiliateDeleteValidator(IEnumerable<IValidatorConsumer<AffiliateDeleteModel>> validators,
        IAffiliateService affiliateService, IMediator mediator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var affiliate = await affiliateService.GetAffiliateById(x.Id);
            if (affiliate == null)
                context.AddFailure("No affiliate found with the specified id");

            var customers = new GetCustomerQuery {
                AffiliateId = affiliate.Id,
                PageSize = 1
            };
            var queryCustomer = (await mediator.Send(customers)).Count();
            if (queryCustomer > 0)
                context.AddFailure("There are exist customers related with affiliate");

            var orders = new GetOrderQuery {
                AffiliateId = affiliate.Id,
                PageSize = 1
            };

            var queryOrder = (await mediator.Send(orders)).Count();
            if (queryOrder > 0)
                context.AddFailure("There are exist orders related with affiliate");
        });
    }
}