using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.News
{
    public class AddNewsCommentModel : BaseModel
    {
        public AddNewsCommentModel()
        {
            Captcha = new CaptchaModel();
        }
        public string Id { get; set; }

        [GrandResourceDisplayName("News.Comments.CommentTitle")]
        public string CommentTitle { get; set; }

        [GrandResourceDisplayName("News.Comments.CommentText")]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }
}