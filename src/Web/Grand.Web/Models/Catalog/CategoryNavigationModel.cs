using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class CategoryNavigationModel : BaseModel
{
    public string CurrentCategoryId { get; set; }
    public List<CategorySimpleModel> Categories { get; set; } = new();

    public class CategoryLineModel : BaseModel
    {
        public string CurrentCategoryId { get; set; }
        public CategorySimpleModel Category { get; set; }
    }
}