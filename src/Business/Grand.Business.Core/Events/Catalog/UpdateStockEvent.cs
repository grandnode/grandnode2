using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog;

public class UpdateStockEvent : INotification
{
    public UpdateStockEvent(Product product)
    {
        Result = product;
    }

    public Product Result { get; }
}