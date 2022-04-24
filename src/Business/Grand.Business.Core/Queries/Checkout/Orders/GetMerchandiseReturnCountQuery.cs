using MediatR;
namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class GetMerchandiseReturnCountQuery : IRequest<int>
    {
        public int RequestStatusId { get; set; }
        public string StoreId { get; set; }
    }
}
