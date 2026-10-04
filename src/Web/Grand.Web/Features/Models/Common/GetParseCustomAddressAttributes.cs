using Grand.Domain.Common;
using Grand.Web.Common.Models;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Common;

public class GetParseCustomAddressAttributes : IRequest<IList<CustomAttribute>>
{
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }
}