using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using MediatR;

namespace Grand.Web.Features.Models.Common;

public class GetParseCustomAddressAttributes : IRequest<IList<CustomAttribute>>
{
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }
}