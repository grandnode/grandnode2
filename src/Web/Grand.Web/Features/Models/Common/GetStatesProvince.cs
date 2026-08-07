using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Common;

public class GetStatesProvince : IRequest<IList<StateProvinceModel>>
{
    public string CountryId { get; set; }
    public bool AddSelectStateItem { get; set; }
}