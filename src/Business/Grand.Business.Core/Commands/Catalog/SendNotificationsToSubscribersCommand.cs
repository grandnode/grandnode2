using Grand.Domain.Catalog;
using Grand.Domain.Common;
using MediatR;

namespace Grand.Business.Core.Commands.Catalog
{
    public class SendNotificationsToSubscribersCommand : IRequest<IList<OutOfStockSubscription>>
    {
        public Product Product { get; set; }
        public IList<CustomAttribute> Attributes { get; set; }
        public string Warehouse { get; set; }
    }
}
