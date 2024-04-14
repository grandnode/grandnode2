//Contributor : MVCContrib

using Grand.Domain;
using Grand.SharedKernel.Attributes;

namespace Grand.Web.Common.Page.Paging;

public abstract class BasePageableModel : IPageableModel
{
    #region Methods

    public void LoadPagedList<T>(IPagedList<T> pagedList)
    {
        FirstItem = pagedList.PageIndex * pagedList.PageSize + 1;
        HasNextPage = pagedList.HasNextPage;
        HasPreviousPage = pagedList.HasPreviousPage;
        LastItem = Math.Min(pagedList.TotalCount, pagedList.PageIndex * pagedList.PageSize + pagedList.PageSize);
        PageNumber = pagedList.PageIndex + 1;
        PageSize = pagedList.PageSize;
        TotalItems = pagedList.TotalCount;
        TotalPages = pagedList.TotalPages;
    }

    #endregion

    #region Properties

    [IgnoreApiUrl] [IgnoreApi] public int FirstItem { get; set; }

    [IgnoreApiUrl] [IgnoreApi] public bool HasNextPage { get; set; }

    [IgnoreApiUrl] [IgnoreApi] public bool HasPreviousPage { get; set; }

    [IgnoreApiUrl] [IgnoreApi] public int LastItem { get; set; }

    [IgnoreApiUrl]
    [IgnoreApi]
    public int PageIndex {
        get {
            if (PageNumber > 0)
                return PageNumber - 1;

            return 0;
        }
    }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    [IgnoreApiUrl] public int TotalItems { get; set; }

    [IgnoreApiUrl] public int TotalPages { get; set; }

    #endregion
}