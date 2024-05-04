using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs;

public class BlogPostYearModel : BaseModel
{
    public int Year { get; set; }
    public IList<BlogPostMonthModel> Months { get; set; } = new List<BlogPostMonthModel>();
}

public class BlogPostMonthModel : BaseModel
{
    public int Month { get; set; }

    public int BlogPostCount { get; set; }
}