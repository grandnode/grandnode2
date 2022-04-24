using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog
{
    public class UpdateStockEvent : INotification
    {
        private readonly Product _product;

        public UpdateStockEvent(Product product)
        {
            _product = product;
        }

        public Product Result { get { return _product; } }
    }
}
