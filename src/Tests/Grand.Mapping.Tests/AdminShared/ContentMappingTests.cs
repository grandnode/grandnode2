using Grand.Mapping;
using Grand.Domain.Blogs;
using Grand.Domain.Courses;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Blogs;
using Grand.Web.AdminShared.Models.Courses;
using Grand.Web.AdminShared.Models.Knowledgebase;
using Grand.Web.AdminShared.Models.News;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.AdminShared.Models.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class ContentMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<BlogCategoryProfile>();
            cfg.AddProfile<BlogPostProfile>();
            cfg.AddProfile<BlogSettingsProfile>();
            cfg.AddProfile<NewsItemProfile>();
            cfg.AddProfile<NewsSettingsProfile>();
            cfg.AddProfile<PageProfile>();
            cfg.AddProfile<KnowledgebaseCategoryProfile>();
            cfg.AddProfile<KnowledgebaseSettingsProfile>();
            cfg.AddProfile<CourseProfile>();
            cfg.AddProfile<CourseLevelProfile>();
            cfg.AddProfile<CourseLessonProfile>();
            cfg.AddProfile<CourseSubjectProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── BlogCategory ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task BlogCategory_ToModel()
    {
        var source = new BlogCategory {
            Id = "bc-001",
            Name = "Tech News",
            SeName = "tech-news",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<BlogCategoryModel>(source));
    }

    [TestMethod]
    public Task BlogCategoryModel_ToDomain()
    {
        var model = new BlogCategoryModel {
            Name = "Tech News",
            SeName = "tech-news",
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<BlogCategory>(model));
    }

    // ── BlogPost ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task BlogPost_ToModel()
    {
        var source = new BlogPost {
            Id = "bp-001",
            Title = "First Post",
            Body = "Content body",
            BodyOverview = "Overview",
            AllowComments = true,
            Tags = "tech,blog",
            StartDateUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            MetaKeywords = "blog",
            MetaDescription = "Blog post",
            MetaTitle = "First Post",
            SeName = "first-post"
        };
        return Verify(_mapper.Map<BlogPostModel>(source));
    }

    [TestMethod]
    public Task BlogPostModel_ToDomain()
    {
        var model = new BlogPostModel {
            Title = "First Post",
            Body = "Content body",
            BodyOverview = "Overview",
            AllowComments = true,
            Tags = "tech,blog",
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<BlogPost>(model));
    }

    // ── BlogSettings ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task BlogSettings_ToModel()
    {
        var source = new BlogSettings { Enabled = true, PostsPageSize = 10, AllowNotRegisteredUsersToLeaveComments = false };
        return Verify(_mapper.Map<ContentSettingsModel.BlogSettingsModel>(source));
    }

    [TestMethod]
    public Task BlogSettingsModel_ToDomain()
    {
        var model = new ContentSettingsModel.BlogSettingsModel { Enabled = true, PostsPageSize = 10 };
        return Verify(_mapper.Map<BlogSettings>(model));
    }

    // ── NewsItem ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task NewsItem_ToModel()
    {
        var source = new NewsItem {
            Id = "ni-001",
            Title = "Breaking News",
            Short = "Short text",
            Full = "Full news content",
            Published = true,
            AllowComments = true,
            StartDateUtc = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            MetaKeywords = "news",
            MetaDescription = "News",
            MetaTitle = "Breaking News",
            SeName = "breaking-news"
        };
        return Verify(_mapper.Map<NewsItemModel>(source));
    }

    [TestMethod]
    public Task NewsItemModel_ToDomain()
    {
        var model = new NewsItemModel {
            Title = "Breaking News",
            Short = "Short text",
            Full = "Full news content",
            Published = true,
            AllowComments = true,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<NewsItem>(model));
    }

    // ── NewsSettings ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task NewsSettings_ToModel()
    {
        var source = new NewsSettings { Enabled = true, AllowNotRegisteredUsersToLeaveComments = true, NotifyAboutNewNewsComments = false, ShowNewsOnMainPage = true, MainPageNewsCount = 3 };
        return Verify(_mapper.Map<ContentSettingsModel.NewsSettingsModel>(source));
    }

    [TestMethod]
    public Task NewsSettingsModel_ToDomain()
    {
        var model = new ContentSettingsModel.NewsSettingsModel { Enabled = true, ShowNewsOnMainPage = true, MainPageNewsCount = 3 };
        return Verify(_mapper.Map<NewsSettings>(model));
    }

    // ── Page ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Page_ToModel()
    {
        var source = new Page {
            Id = "page-001",
            SystemName = "about-us",
            IncludeInSitemap = true,
            IsPasswordProtected = false,
            Published = true,
            Title = "About Us",
            Body = "<p>About us page</p>",
            MetaKeywords = "about",
            MetaDescription = "About us",
            MetaTitle = "About Us",
            DisplayOrder = 1,
            SeName = "about-us",
            PageLayoutId = "pl-001"
        };
        return Verify(_mapper.Map<PageModel>(source));
    }

    [TestMethod]
    public Task PageModel_ToDomain()
    {
        var model = new PageModel {
            SystemName = "about-us",
            IncludeInSitemap = true,
            Published = true,
            Title = "About Us",
            Body = "<p>About us page</p>",
            DisplayOrder = 1,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Page>(model));
    }

    // ── KnowledgebaseCategory ─────────────────────────────────────────────────

    [TestMethod]
    public Task KnowledgebaseCategory_ToModel()
    {
        var source = new KnowledgebaseCategory {
            Id = "kb-001",
            Name = "FAQ",
            Description = "Frequently asked questions",
            ParentCategoryId = "",
            DisplayOrder = 1,
            Published = true,
            SeName = "faq"
        };
        return Verify(_mapper.Map<KnowledgebaseCategoryModel>(source));
    }

    [TestMethod]
    public Task KnowledgebaseCategoryModel_ToDomain()
    {
        var model = new KnowledgebaseCategoryModel {
            Name = "FAQ",
            Description = "Frequently asked questions",
            DisplayOrder = 1,
            Published = true,
            Stores = ["store-001"],
            CustomerGroups = ["grp-001"]
        };
        return Verify(_mapper.Map<KnowledgebaseCategory>(model));
    }

    [TestMethod]
    public Task KnowledgebaseArticle_ToModel()
    {
        var source = new KnowledgebaseArticle {
            Id = "kba-001",
            Name = "How to order?",
            Content = "Step-by-step guide",
            ParentCategoryId = "kb-001",
            DisplayOrder = 1,
            Published = true,
            SeName = "how-to-order"
        };
        return Verify(_mapper.Map<KnowledgebaseArticleModel>(source));
    }

    [TestMethod]
    public Task KnowledgebaseArticleModel_ToDomain()
    {
        var model = new KnowledgebaseArticleModel {
            Name = "How to order?",
            Content = "Step-by-step guide",
            ParentCategoryId = "kb-001",
            DisplayOrder = 1,
            Published = true,
            Stores = ["store-001"],
            CustomerGroups = ["grp-001"]
        };
        return Verify(_mapper.Map<KnowledgebaseArticle>(model));
    }

    // ── KnowledgebaseSettings ─────────────────────────────────────────────────

    [TestMethod]
    public Task KnowledgebaseSettings_ToModel()
    {
        var source = new KnowledgebaseSettings { Enabled = true, AllowNotRegisteredUsersToLeaveComments = false };
        return Verify(_mapper.Map<ContentSettingsModel.KnowledgebaseSettingsModel>(source));
    }

    [TestMethod]
    public Task KnowledgebaseSettingsModel_ToDomain()
    {
        var model = new ContentSettingsModel.KnowledgebaseSettingsModel { Enabled = true };
        return Verify(_mapper.Map<KnowledgebaseSettings>(model));
    }

    // ── Course ────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Course_ToModel()
    {
        var source = new Course {
            Id = "course-001",
            Name = "C# Fundamentals",
            ShortDescription = "Learn C#",
            Description = "Full course",
            LevelId = "cl-001",
            Published = true,
            DisplayOrder = 1,
            SeName = "csharp-fundamentals",
            ProductId = "prod-001"
        };
        return Verify(_mapper.Map<CourseModel>(source));
    }

    [TestMethod]
    public Task CourseModel_ToDomain()
    {
        var model = new CourseModel {
            Name = "C# Fundamentals",
            ShortDescription = "Learn C#",
            Description = "Full course",
            LevelId = "cl-001",
            Published = true,
            DisplayOrder = 1,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Course>(model));
    }

    // ── CourseLevel ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task CourseLevel_ToModel()
    {
        var source = new CourseLevel { Id = "cl-001", Name = "Beginner", DisplayOrder = 1 };
        return Verify(_mapper.Map<CourseLevelModel>(source));
    }

    [TestMethod]
    public Task CourseLevelModel_ToDomain()
    {
        var model = new CourseLevelModel { Name = "Beginner", DisplayOrder = 1 };
        return Verify(_mapper.Map<CourseLevel>(model));
    }

    // ── CourseLesson ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task CourseLesson_ToModel()
    {
        var source = new CourseLesson {
            Id = "lesson-001",
            Name = "Introduction",
            ShortDescription = "Lesson overview",
            SubjectId = "subj-001",
            CourseId = "course-001",
            DisplayOrder = 1,
            Published = true,
            AttachmentId = "attach-001",
            VideoFile = "intro.mp4"
        };
        return Verify(_mapper.Map<CourseLessonModel>(source));
    }

    [TestMethod]
    public Task CourseLessonModel_ToDomain()
    {
        var model = new CourseLessonModel {
            Name = "Introduction",
            ShortDescription = "Lesson overview",
            SubjectId = "subj-001",
            CourseId = "course-001",
            DisplayOrder = 1,
            Published = true
        };
        return Verify(_mapper.Map<CourseLesson>(model));
    }

    // ── CourseSubject ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task CourseSubject_ToModel()
    {
        var source = new CourseSubject { Id = "subj-001", Name = "Chapter 1", CourseId = "course-001", DisplayOrder = 1 };
        return Verify(_mapper.Map<CourseSubjectModel>(source));
    }

    [TestMethod]
    public Task CourseSubjectModel_ToDomain()
    {
        var model = new CourseSubjectModel { Name = "Chapter 1", CourseId = "course-001", DisplayOrder = 1 };
        return Verify(_mapper.Map<CourseSubject>(model));
    }
}
