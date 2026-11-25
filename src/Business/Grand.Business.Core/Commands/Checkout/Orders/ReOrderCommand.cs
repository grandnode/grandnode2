using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ReOrderCommand : IRequest<IList<string>>
{
    public required Order Order { get; set; }

    /// <summary>
    ///     Language the returned warnings are written in
    /// </summary>
    public required Language Language { get; set; }
}