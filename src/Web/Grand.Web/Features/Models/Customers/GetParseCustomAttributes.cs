using Grand.Domain.Common;
using Grand.Web.Common.Models;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetParseCustomAttributes : IRequest<IList<CustomAttribute>>
{
    public required IList<CustomAttributeModel> SelectedAttributes { get; set; }

    public List<CustomAttribute> CustomerCustomAttribute { get; set; } = new();
}