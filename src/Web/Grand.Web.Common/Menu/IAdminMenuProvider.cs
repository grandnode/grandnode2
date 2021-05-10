using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Grand.Web.Common.Menu
{
    /// <summary>
    /// Interface for provider which have some items in the admin area menu
    /// </summary>
    public interface IAdminMenuProvider : IProvider
    {
        /// <summary>
        /// Manage sitemap. You can use "SystemName" of menu items to manage existing sitemap or add a new menu item.
        /// </summary>
        /// <param name="rootNode">Root node of the sitemap.</param>
        Task ManageSiteMap(SiteMapNode rootNode);
    }
}
