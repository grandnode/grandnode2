using Grand.Web.Common.Page.Paging;

namespace Grand.Web.Models.Blogs;

public class BlogPagingFilteringModel : BasePageableModel
{
    #region Methods

    public virtual DateTime? GetParsedMonth()
    {
        if (string.IsNullOrEmpty(Month)) return null;
        var tempDate = Month.Split(['-']);
        if (tempDate.Length != 2) return null;
        if (!int.TryParse(tempDate[0], out var year) || year is < 1 or > 9999) return null;
        if (!int.TryParse(tempDate[1], out var month) || month is < 1 or > 12) return null;

        return new DateTime(year, month, 1);
    }

    public virtual DateTime? GetFromMonth()
    {
        var filterByMonth = GetParsedMonth();
        return filterByMonth;
    }

    public virtual DateTime? GetToMonth()
    {
        var filterByMonth = GetParsedMonth();
        return filterByMonth?.AddMonths(1).AddSeconds(-1);
    }

    #endregion

    #region Properties

    public string Month { get; set; }
    public string Tag { get; set; }
    public string CategorySeName { get; set; }
    public string SearchKeyword { get; set; }

    #endregion
}