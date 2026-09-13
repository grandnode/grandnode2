namespace Grand.Domain;

/// <summary>
///     Paged list
/// </summary>
/// <typeparam name="T">T</typeparam>
[Serializable]
public class PagedList<T> : List<T>, IPagedList<T>
{
    public PagedList()
    {
    }

    public PagedList(IEnumerable<T> source, int pageIndex, int pageSize)
    {
        Initialize(source, pageIndex, pageSize);
    }

    public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
    {
        Initialize(source, pageIndex, pageSize, totalCount);
    }

    public int PageIndex { get; protected set; }
    public int PageSize { get; protected set; }
    public int TotalCount { get; protected set; }
    public int TotalPages { get; protected set; }

    public bool HasPreviousPage => PageIndex > 0;

    public bool HasNextPage => PageIndex + 1 < TotalPages;

    private void Initialize(IEnumerable<T> source, int pageIndex, int pageSize, int? totalCount = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageSize <= 0)
            pageSize = 1;

        TotalCount = totalCount ?? source.Count();

        if (pageSize > 0)
        {
            TotalPages = TotalCount / pageSize;
            if (TotalCount % pageSize > 0)
                TotalPages++;
        }

        PageSize = pageSize;
        PageIndex = pageIndex;
        source = totalCount == null ? source.Skip(pageIndex * pageSize).Take(pageSize) : source;
        AddRange(source);
    }

}