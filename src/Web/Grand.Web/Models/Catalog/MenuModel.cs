using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Models.Catalog
{
    public partial class MenuModel : BaseModel
    {
        public MenuModel()
        {
            Categories = new List<CategorySimpleModel>();
            Pages = new List<MenuPageModel>();
            Brands = new List<MenuBrandModel>();
            Collections = new List<MenuCollectionModel> ();
        }

        public IList<CategorySimpleModel> Categories { get; set; }
        public IList<MenuPageModel> Pages { get; set; }
        public IList<MenuBrandModel> Brands { get; set; }
        public IList<MenuCollectionModel> Collections { get; set; }

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
}