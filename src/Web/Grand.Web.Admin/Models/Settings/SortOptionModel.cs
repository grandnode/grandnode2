using Grand.Infrastructure.ModelBinding;

namespace Grand.Web.Admin.Models.Settings;

public class SortOptionModel
{
    public virtual int Id { get; set; }

    [GrandResourceDisplayName("Admin.Settings.Catalog.SortOptions.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Settings.Catalog.SortOptions.IsActive")]
    public bool IsActive { get; set; }

    [GrandResourceDisplayName("Admin.Settings.Catalog.SortOptions.DisplayOrder")]
    public int DisplayOrder { get; set; }
}