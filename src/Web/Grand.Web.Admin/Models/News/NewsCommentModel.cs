using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Admin.Models.News
{
    public partial class NewsCommentModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.NewsItem")]
        public string NewsItemId { get; set; }
        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.NewsItem")]
        
        public string NewsItemTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.Customer")]
        public string CustomerId { get; set; }
        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.Customer")]
        public string CustomerInfo { get; set; }

        
        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.CommentTitle")]
        public string CommentTitle { get; set; }

        
        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.CommentText")]
        public string CommentText { get; set; }

        [GrandResourceDisplayName("Admin.Content.News.Comments.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

    }
}