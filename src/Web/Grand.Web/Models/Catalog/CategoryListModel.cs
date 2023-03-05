namespace Grand.Web.Models.Catalog;

public class CategoryListModel
{
    public CategoryListModel()
    {
        PagingModel = new CategoryPagingModel();
        CategoriesModel = new List<CategoryModel>();
    }
    public CategoryPagingModel PagingModel { get; set; }
    public IList<CategoryModel> CategoriesModel { get; set; }
}