using Grand.Infrastructure.Models;

namespace Grand.Web.Models.News
{
    public class NewsCommentModel : BaseEntityModel
    {
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CommentTitle { get; set; }

        public string CommentText { get; set; }

        public DateTime CreatedOn { get; set; }

    }
}