using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public class ContentSettingsModel : BaseModel
    {

        public ContentSettingsModel()
        {
            BlogSettings = new BlogSettingsModel();
            NewsSettings = new NewsSettingsModel();
            KnowledgebaseSettings = new KnowledgebaseSettingsModel();

        }
        public string ActiveStore { get; set; }
        public BlogSettingsModel BlogSettings { get; set; }
        public NewsSettingsModel NewsSettings { get; set; }
        public KnowledgebaseSettingsModel KnowledgebaseSettings { get; set; }

        public partial class BlogSettingsModel : BaseModel
        {
            
            [GrandResourceDisplayName("Admin.Settings.Blog.Enabled")]
            public bool Enabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.PostsPageSize")]
            public int PostsPageSize { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.AllowNotRegisteredUsersToLeaveComments")]
            public bool AllowNotRegisteredUsersToLeaveComments { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.NotifyAboutNewBlogComments")]
            public bool NotifyAboutNewBlogComments { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.NumberOfTags")]
            public int NumberOfTags { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.ShowBlogOnHomePage")]
            public bool ShowBlogOnHomePage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.HomePageBlogCount")]
            public int HomePageBlogCount { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.MaxTextSizeHomePage")]
            public int MaxTextSizeHomePage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Blog.ShowBlogPostsInSearchAutoComplete")]
            public bool ShowBlogPostsInSearchAutoComplete { get; set; }
        }

        public partial class NewsSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.News.Enabled")]
            public bool Enabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.News.AllowNotRegisteredUsersToLeaveComments")]
            public bool AllowNotRegisteredUsersToLeaveComments { get; set; }

            [GrandResourceDisplayName("Admin.Settings.News.NotifyAboutNewNewsComments")]
            public bool NotifyAboutNewNewsComments { get; set; }

            [GrandResourceDisplayName("Admin.Settings.News.ShowNewsOnMainPage")]
            public bool ShowNewsOnMainPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.News.MainPageNewsCount")]
            public int MainPageNewsCount { get; set; }

            [GrandResourceDisplayName("Admin.Settings.News.NewsArchivePageSize")]
            public int NewsArchivePageSize { get; set; }

        }

        public partial class KnowledgebaseSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.GeneralCommon.KnowledgebaseSettings.Enabled")]
            public bool Enabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Knowledgebase.AllowNotRegisteredUsersToLeaveComments")]
            public bool AllowNotRegisteredUsersToLeaveComments { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Knowledgebase.NotifyAboutNewArticleComments")]
            public bool NotifyAboutNewArticleComments { get; set; }
        }

    }
}
