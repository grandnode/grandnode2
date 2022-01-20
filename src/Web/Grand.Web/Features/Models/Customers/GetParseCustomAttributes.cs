
using Grand.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Features.Models.Customers
{
    public class GetParseCustomAttributes : IRequest<IList<CustomAttribute>>
    {
        public GetParseCustomAttributes()
        {
            CustomerCustomAttribute = new List<CustomAttribute>();
        }
        public IFormCollection Form { get; set; }

        public List<CustomAttribute> CustomerCustomAttribute { get; set; }
    }
}
