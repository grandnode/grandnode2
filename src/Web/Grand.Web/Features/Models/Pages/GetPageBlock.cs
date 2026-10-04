using Grand.Web.Models.Pages;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Pages;

public class GetPageBlock : IRequest<PageModel>
{
    public string SystemName { get; set; }
    public string PageId { get; set; }
    public string Password { get; set; }

    /// <summary>
    ///     Return the page even when it is unpublished or outside its date range (preview by a page manager)
    /// </summary>
    public bool ShowHidden { get; set; }
}