using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Blogs
{
    public class AddBlogCommentModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Blog.Comments.CommentText")]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
        
        public ICaptchaValidModel Captcha { get; set; } = new CaptchaModel();
    }
}