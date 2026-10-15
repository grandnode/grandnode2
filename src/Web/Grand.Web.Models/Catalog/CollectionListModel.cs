namespace Grand.Web.Models.Catalog;

public class CollectionListModel
{
    public CollectionPagingModel PagingModel { get; set; } = new();
    public IList<CollectionModel> CollectionModel { get; set; } = new List<CollectionModel>();
}