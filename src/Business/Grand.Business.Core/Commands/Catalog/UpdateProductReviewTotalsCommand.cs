using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Catalog;

public class UpdateProductReviewTotalsCommand : IRequest<bool>
{
    public Product Product { get; set; }
}