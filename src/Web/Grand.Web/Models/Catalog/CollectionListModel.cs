namespace Grand.Web.Models.Catalog;

public class CollectionListModel
{
    public CollectionListModel()
    {
        PagingModel = new CollectionPagingModel();
        CollectionModel = new List<CollectionModel>();
    }
    public CollectionPagingModel PagingModel { get; set; }
    public IList<CollectionModel> CollectionModel { get; set; }
}