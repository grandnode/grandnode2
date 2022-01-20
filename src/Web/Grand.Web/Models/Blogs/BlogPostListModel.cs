﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Blogs
{
    public partial class BlogPostListModel : BaseModel
    {
        public BlogPostListModel()
        {
            PagingFilteringContext = new BlogPagingFilteringModel();
            BlogPosts = new List<BlogPostModel>();
            PictureModel = new PictureModel();
        }
        public PictureModel PictureModel { get; set; }
        public string WorkingLanguageId { get; set; }
        public BlogPagingFilteringModel PagingFilteringContext { get; set; }
        public IList<BlogPostModel> BlogPosts { get; set; }
        public string SearchKeyword { get; set; }
    }
}