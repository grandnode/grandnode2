namespace Grand.Web.Common.DataSource;

public class DataSourceRequest
{
    //a grid posts its page and page size itself, so a request could ask for every row of a collection at once
    public const int MaxPageSize = 1000;

    private int _page = 1;
    private int _pageSize = 10;

    public int Page {
        get => _page;
        set => _page = Math.Max(value, 1);
    }

    public int PageSize {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}