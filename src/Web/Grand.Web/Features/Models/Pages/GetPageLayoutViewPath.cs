using Grand.Mediator;

namespace Grand.Web.Features.Models.Pages;

public class GetPageLayoutViewPath : IRequest<string>
{
    public string LayoutId { get; set; }
}