using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class CollectionNavigationModel : BaseModel
{
    public IList<CollectionBriefInfoModel> Collections { get; set; } = new List<CollectionBriefInfoModel>();

    public int TotalCollections { get; set; }
}

public class CollectionBriefInfoModel : BaseEntityModel
{
    public string Name { get; set; }
    public string SeName { get; set; }
    public string Icon { get; set; }
    public bool IsActive { get; set; }
}