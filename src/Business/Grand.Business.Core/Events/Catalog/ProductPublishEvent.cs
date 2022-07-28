using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog
{
    public class ProductPublishEvent : INotification
    {
        public ProductPublishEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; private set; }
    }
}
