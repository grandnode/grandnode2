using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs
{
    public class BlogPostYearModel : BaseModel
    {
        public BlogPostYearModel()
        {
            Months = new List<BlogPostMonthModel>();
        }
        public int Year { get; set; }
        public IList<BlogPostMonthModel> Months { get; set; }
    }
    public class BlogPostMonthModel : BaseModel
    {
        public int Month { get; set; }

        public int BlogPostCount { get; set; }
    }
}