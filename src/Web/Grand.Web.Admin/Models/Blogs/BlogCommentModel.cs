using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Admin.Models.Blogs
{
    public partial class BlogCommentModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.BlogPost")]
        public string BlogPostId { get; set; }
        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.BlogPost")]
        
        public string BlogPostTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.Customer")]
        public string CustomerId { get; set; }
        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.Customer")]
        public string CustomerInfo { get; set; }

        
        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.Comment")]
        public string Comment { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.Comments.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

    }
}