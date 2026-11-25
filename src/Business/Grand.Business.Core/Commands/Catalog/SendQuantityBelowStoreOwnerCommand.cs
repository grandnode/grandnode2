using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Catalog;

public class SendQuantityBelowStoreOwnerCommand : IRequest<bool>
{
    public required Product Product { get; set; }
    public ProductAttributeCombination? ProductAttributeCombination { get; set; }
}