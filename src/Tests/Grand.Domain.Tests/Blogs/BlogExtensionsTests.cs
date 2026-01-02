using Grand.Domain.Blogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Blogs;

[TestClass]
public class BlogExtensionsTests
{
    [TestMethod]
    public void ParseTagsTest()
    {
        var blogPost = new BlogPost {
            Tags = "e-commerce, blog, moey"
        };

        Assert.HasCount(3, blogPost.ParseTags());
    }

    [TestMethod]
    public void GetPostsByDateTest()
    {
        var blogPosts = new List<BlogPost> {
            new() { StartDateUtc = new DateTime(2010, 01, 01) },
            new() { StartDateUtc = new DateTime(2010, 02, 01) },
            new() { StartDateUtc = new DateTime(2010, 03, 01) }
        };
        Assert.HasCount(2, blogPosts.GetPostsByDate(new DateTime(2010, 01, 01), new DateTime(2010, 02, 28)));
    }
}