using Grand.Domain.Common;
using Grand.Web.Common.Models;
using MediatR;

namespace Grand.Web.Features.Models.Customers;

public class GetParseCustomAttributes : IRequest<IList<CustomAttribute>>
{
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }

    public List<CustomAttribute> CustomerCustomAttribute { get; set; } = new();
}