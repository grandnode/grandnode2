using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.System.Common;

public class GetSitemapXmlCommand : IRequest<string>
{
    public required Store Store { get; set; }
    public required Language Language { get; set; }
}