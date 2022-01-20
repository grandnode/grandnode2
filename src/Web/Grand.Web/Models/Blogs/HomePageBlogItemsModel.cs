﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Blogs
{
    public class HomePageBlogItemsModel: BaseModel
    {
        public HomePageBlogItemsModel()
        {
            Items = new List<BlogItemModel>();
        }

        public IList<BlogItemModel> Items { get; set; }

        public class BlogItemModel : BaseModel
        {
            public string SeName { get; set; }
            public string Title { get; set; }
            public PictureModel PictureModel { get; set; }
            public string Short { get; set; }
            public string Full { get; set; }
            public string Category { get; set; }
            public DateTime CreatedOn { get; set; }
        }
    }
}
