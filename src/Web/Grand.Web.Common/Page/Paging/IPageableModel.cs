//Contributor : MVCContrib

using Grand.SharedKernel.Attributes;

namespace Grand.Web.Common.Page.Paging;

/// <summary>
///     A collection of objects that has been split into pages.
/// </summary>
public interface IPageableModel
{
    /// <summary>
    ///     The current page index (starts from 0)
    /// </summary>
    [IgnoreApi]
    int PageIndex { get; }

    /// <summary>
    ///     The current page number (starts from 1)
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    ///     The number of items in each page.
    /// </summary>
    int PageSize { get; }

    /// <summary>
    ///     The total number of items.
    /// </summary>
    int TotalItems { get; }

    /// <summary>
    ///     The total number of pages.
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    ///     The index of the first item in the page.
    /// </summary>
    [IgnoreApi]
    int FirstItem { get; }

    /// <summary>
    ///     The index of the last item in the page.
    /// </summary>
    [IgnoreApi]
    int LastItem { get; }

    /// <summary>
    ///     Whether there are pages before the current page.
    /// </summary>
    [IgnoreApi]
    bool HasPreviousPage { get; }

    /// <summary>
    ///     Whether there are pages after the current page.
    /// </summary>
    [IgnoreApi]
    bool HasNextPage { get; }
}

/// <summary>
///     Generic form of <see cref="IPageableModel" />
/// </summary>
/// <typeparam name="T">Type of object being paged</typeparam>
public interface IPagination<T> : IPageableModel;