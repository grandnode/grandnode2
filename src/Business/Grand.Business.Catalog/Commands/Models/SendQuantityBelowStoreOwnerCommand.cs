using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Commands.Models
{
    public class SendQuantityBelowStoreOwnerCommand : IRequest<bool>
    {
        public Product Product { get; set; }
        public ProductAttributeCombination ProductAttributeCombination { get; set; }

    }
}
