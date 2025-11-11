using Grand.Domain.Pages;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.Store.Interfaces;

/// <summary>
/// Store-specific extension of IPageViewModelService
/// Adds Store-specific functionality like copying system pages
/// </summary>
public interface IStorePageViewModelService : IPageViewModelService
{
    /// <summary>
    /// Copy a page (typically a system page) for the current store
    /// </summary>
    /// <param name="sourcePageId">Source page ID to copy</param>
    /// <returns>Newly created page</returns>
    Task<Page> CopyPageModel(string sourcePageId);
}
