using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog
{
    public class ProductUnPublishEvent : INotification
    {
        public ProductUnPublishEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; private set; }
    }
}
