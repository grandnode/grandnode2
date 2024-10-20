namespace Grand.Domain.Blogs;

public static class BlogExtensions
{
    public static string[] ParseTags(this BlogPost blogPost)
    {
        ArgumentNullException.ThrowIfNull(blogPost);

        var parsedTags = new List<string>();
        if (!string.IsNullOrEmpty(blogPost.Tags))
        {
            var tags2 = blogPost.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tag2 in tags2)
            {
                var tmp = tag2.Trim();
                if (!string.IsNullOrEmpty(tmp))
                    parsedTags.Add(tmp);
            }
        }

        return parsedTags.ToArray();
    }

    /// <summary>
    ///     Returns all posts published between the two dates.
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="dateFrom">Date from</param>
    /// <param name="dateTo">Date to</param>
    /// <returns>Filtered posts</returns>
    public static IList<BlogPost> GetPostsByDate(this IList<BlogPost> source,
        DateTime dateFrom, DateTime dateTo)
    {
        return source.Where(p =>
                dateFrom.Date <= (p.StartDateUtc ?? p.CreatedOnUtc) &&
                (p.StartDateUtc ?? p.CreatedOnUtc).Date <= dateTo)
            .ToList();
    }
}