﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Blogs.Tests
{
    [TestClass()]
    public class BlogExtensionsTests
    {
        [TestMethod()]
        public void ParseTagsTest()
        {
            var blogPost = new BlogPost() {
                Tags = "e-commerce, blog, moey"
            };

            Assert.AreEqual(3, blogPost.ParseTags().Count());
        }

        [TestMethod()]
        public void GetPostsByDateTest()
        {
            var blogPosts = new List<BlogPost>() {
                new BlogPost(){ StartDateUtc = new DateTime(2010,01,01) },
                new BlogPost(){ StartDateUtc = new DateTime(2010,02,01) },
                new BlogPost(){ StartDateUtc = new DateTime(2010,03,01) },
            };
            Assert.AreEqual(2, blogPosts.GetPostsByDate(new DateTime(2010, 01, 01), new DateTime(2010, 02, 28)).Count());
        }
    }
}