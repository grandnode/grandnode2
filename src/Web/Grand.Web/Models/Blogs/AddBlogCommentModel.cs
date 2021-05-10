using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs
{
    public partial class AddBlogCommentModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Blog.Comments.CommentText")]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}