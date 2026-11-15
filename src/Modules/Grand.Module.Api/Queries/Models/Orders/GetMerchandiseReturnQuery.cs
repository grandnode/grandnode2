using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Module.Api.Queries.Models.Orders;

public class GetMerchandiseReturnQuery : IRequest<MerchandiseReturn>
{
    public string Id { get; set; }
}
