﻿namespace Grand.Domain.Blogs
{
    public static class BlogExtensions
    {
        public static string[] ParseTags(this BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            var parsedTags = new List<string>();
            if (!String.IsNullOrEmpty(blogPost.Tags))
            {
                string[] tags2 = blogPost.Tags.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string tag2 in tags2)
                {
                    var tmp = tag2.Trim();
                    if (!String.IsNullOrEmpty(tmp))
                        parsedTags.Add(tmp);
                }
            }
            return parsedTags.ToArray();
        }

        /// <summary>
        /// Returns all posts published between the two dates.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="dateFrom">Date from</param>
        /// <param name="dateTo">Date to</param>
        /// <returns>Filtered posts</returns>
        public static IList<BlogPost> GetPostsByDate(this IList<BlogPost> source,
            DateTime dateFrom, DateTime dateTo)
        {
            return source.Where(p => dateFrom.Date <= (p.StartDateUtc ?? p.CreatedOnUtc) && (p.StartDateUtc ?? p.CreatedOnUtc).Date <= dateTo).ToList();
        }
    }
}
