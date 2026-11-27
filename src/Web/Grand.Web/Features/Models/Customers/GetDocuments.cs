using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetDocuments : IRequest<DocumentsModel>
{
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public DocumentPagingModel Command { get; set; } = new();
}