using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog;

public class UpdateProductOnCartEvent : INotification
{
    public UpdateProductOnCartEvent(Product product)
    {
        Product = product;
    }

    public Product Product { get; }
}