using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Products;

public class SendProductAskQuestionMessageCommand : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public required Language Language { get; set; }
    public required Product Product { get; set; }
    public required ProductAskQuestionModel Model { get; set; }
    public string? RemoteIpAddress { get; set; }
}