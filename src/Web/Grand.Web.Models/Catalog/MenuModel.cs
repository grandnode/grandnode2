using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class MenuModel : BaseModel
{
    public IList<CategorySimpleModel> Categories { get; set; } = new List<CategorySimpleModel>();
    public IList<MenuPageModel> Pages { get; set; } = new List<MenuPageModel>();
    public IList<MenuBrandModel> Brands { get; set; } = new List<MenuBrandModel>();
    public IList<MenuCollectionModel> Collections { get; set; } = new List<MenuCollectionModel>();

    public bool BlogEnabled { get; set; }
    public bool NewProductsEnabled { get; set; }

    public bool DisplayHomePageMenu { get; set; }
    public bool DisplayNewProductsMenu { get; set; }
    public bool DisplaySearchMenu { get; set; }
    public bool DisplayCustomerMenu { get; set; }
    public bool DisplayBlogMenu { get; set; }
    public bool DisplayContactUsMenu { get; set; }

    #region Nested classes

    public class MenuPageModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }
    }

    public class MenuBrandModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string SeName { get; set; }
    }

    public class MenuCollectionModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string SeName { get; set; }
    }


    public class CategoryLineModel : BaseModel
    {
        public int Level { get; set; }
        public bool ResponsiveMobileMenu { get; set; }
        public CategorySimpleModel Category { get; set; }
    }

    #endregion
}