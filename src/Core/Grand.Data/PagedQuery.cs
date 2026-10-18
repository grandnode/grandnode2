namespace Grand.Data;

/// <summary>
///     Paging arithmetic shared by the repository implementations of PagedAsync
/// </summary>
internal static class PagedQuery
{
    /// <summary>
    ///     Number of items to skip; query models default PageSize to int.MaxValue, so the product can overflow
    /// </summary>
    public static int Skip(int pageIndex, int pageSize)
    {
        return (int)Math.Clamp((long)pageIndex * pageSize, 0, int.MaxValue);
    }

    /// <summary>
    ///     Total count implied by the fetched page, or null when only a count query can tell
    /// </summary>
    /// <remarks>
    ///     A page that came back short is the last one, so everything before it plus its own items is the total.
    ///     This saves the count for full loads (PageSize = int.MaxValue) and for lookups that find nothing.
    /// </remarks>
    public static int? TotalFromPage(int skip, int pageSize, int itemCount)
    {
        if (itemCount >= pageSize)
            return null;
        if (itemCount == 0 && skip > 0)
            return null;

        return skip + itemCount;
    }
}
