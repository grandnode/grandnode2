using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

public class InsertOrderNoteCommand : IRequest<OrderNote>
{
    public required Order Order { get; set; }
    public required Language Language { get; set; }
    public required AddOrderNoteModel OrderNote { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
}