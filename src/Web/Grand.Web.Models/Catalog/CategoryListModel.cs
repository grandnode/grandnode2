namespace Grand.Web.Models.Catalog;

public class CategoryListModel
{
    public CategoryPagingModel PagingModel { get; set; } = new();
    public IList<CategoryModel> CategoriesModel { get; set; } = new List<CategoryModel>();
}