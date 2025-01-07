using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.News;

public class NewsItemModel : BaseEntityModel
{
    public string NewsItemId { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public string Title { get; set; }
    public PictureModel PictureModel { get; set; } = new();
    public string Short { get; set; }
    public string Full { get; set; }
    public bool AllowComments { get; set; }
    public int NumberOfComments { get; set; }
    public DateTime CreatedOn { get; set; }
    public IList<NewsCommentModel> Comments { get; set; } = new List<NewsCommentModel>();
    public AddNewsCommentModel AddNewComment { get; set; } = new();
}