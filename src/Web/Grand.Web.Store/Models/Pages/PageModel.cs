using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Store.Models.Pages;

public class PageModel : BaseEntityModel, ILocalizedModel<PageLocalizedModel>
{
    [GrandResourceDisplayName("Store.Content.Pages.Fields.SystemName")]
    public string SystemName { get; set; }

    [GrandResourceDisplayName("Store.Content.Pages.Fields.Title")]
    public string Title { get; set; }

    [GrandResourceDisplayName("Store.Content.Pages.Fields.Body")]
    public string Body { get; set; }

    [GrandResourceDisplayName("Store.Content.Pages.Fields.PageLayout")]
    public string PageLayoutId { get; set; }

    public IList<PageLocalizedModel> Locales { get; set; } = new List<PageLocalizedModel>();

    [GrandResourceDisplayName("Store.Content.Pages.Fields.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Store.Content.Pages.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public bool IsSystemPage { get; set; }
}

public class PageLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Store.Content.Pages.Fields.Title")]
    public string Title { get; set; }

    [GrandResourceDisplayName("Store.Content.Pages.Fields.Body")]
    public string Body { get; set; }

    public string LanguageId { get; set; }
}
