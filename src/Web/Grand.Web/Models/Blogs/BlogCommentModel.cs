using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs
{
    public partial class BlogCommentModel : BaseEntityModel
    {
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CommentText { get; set; }

        public DateTime CreatedOn { get; set; }

    }
}