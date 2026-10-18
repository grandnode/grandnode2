using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Queries.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Affiliates;
using Grand.Mediator;

namespace Grand.Web.AdminShared.Validators.Affiliate;

public class AffiliateDeleteValidator : BaseGrandValidator<AffiliateDeleteModel>
{
    public AffiliateDeleteValidator(IEnumerable<IValidatorConsumer<AffiliateDeleteModel>> validators,
        IAffiliateService affiliateService, IMediator mediator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, cancellationToken) =>
        {
            var affiliate = await affiliateService.GetAffiliateById(x.Id);
            if (affiliate == null)
            {
                context.AddFailure("No affiliate found with the specified id");
                return;
            }

            var customers = new GetCustomerQuery {
                AffiliateId = affiliate.Id,
                PageSize = 1
            };
            var queryCustomer = (await mediator.Send(customers, cancellationToken)).TotalCount;
            if (queryCustomer > 0)
                context.AddFailure("There are exist customers related with affiliate");

            var orders = new GetOrderQuery {
                AffiliateId = affiliate.Id,
                PageSize = 1
            };

            var queryOrder = (await mediator.Send(orders, cancellationToken)).TotalCount;
            if (queryOrder > 0)
                context.AddFailure("There are exist orders related with affiliate");
        });
    }
}