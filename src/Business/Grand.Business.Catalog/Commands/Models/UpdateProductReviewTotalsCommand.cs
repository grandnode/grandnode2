using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Commands.Models
{
    public class UpdateProductReviewTotalsCommand : IRequest<bool>
    {
        public Product Product { get; set; }
    }
}
