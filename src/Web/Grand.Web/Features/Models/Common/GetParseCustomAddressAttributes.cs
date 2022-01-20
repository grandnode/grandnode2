using Grand.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Features.Models.Common
{
    public class GetParseCustomAddressAttributes : IRequest<IList<CustomAttribute>>
    {
        public IFormCollection Form { get; set; }
    }
}
