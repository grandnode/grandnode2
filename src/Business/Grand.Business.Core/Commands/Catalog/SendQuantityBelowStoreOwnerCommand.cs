using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Catalog;

public class SendQuantityBelowStoreOwnerCommand : IRequest<bool>
{
    public Product Product { get; set; }
    public ProductAttributeCombination ProductAttributeCombination { get; set; }
}