using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.News;

public class NewsItemListModel : BaseModel
{
    public string WorkingLanguageId { get; set; }
    public NewsPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public IList<NewsItemModel> NewsItems { get; set; } = new List<NewsItemModel>();

    public class NewsItemModel : BaseModel
    {
        public string Id { get; set; }
        public string SeName { get; set; }
        public string Title { get; set; }
        public PictureModel PictureModel { get; set; } = new();
        public string Short { get; set; }
        public string Full { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}