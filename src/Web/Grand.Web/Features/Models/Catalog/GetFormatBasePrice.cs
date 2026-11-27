using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetFormatBasePrice : IRequest<string?>
{
    public required Currency Currency { get; set; }
    public required Product Product { get; set; }
    public double? ProductPrice { get; set; }
}