using Grand.Domain.Localization;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Business.System.Commands.Models.Common
{
    public class GetSitemapXmlCommand : IRequest<string>
    {
        public Store Store { get; set; }
        public Language Language { get; set; }
    }
}
