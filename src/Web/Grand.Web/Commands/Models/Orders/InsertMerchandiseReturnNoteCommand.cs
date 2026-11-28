using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     A message the customer writes to the store on their merchandise return, with an image when they add one; the
///     reason it was refused, as a resource key, or null
/// </summary>
public class InsertMerchandiseReturnNoteCommand : IRequest<string?>
{
    public required MerchandiseReturn MerchandiseReturn { get; set; }
    public required Order Order { get; set; }
    public string? Note { get; set; }
    public IFormFile? File { get; set; }
    public required Customer Customer { get; set; }
    public required DomainHost Host { get; set; }
}
