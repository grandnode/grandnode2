using Grand.Web.Common.Page.Paging;

namespace Grand.Web.Models.Blogs
{
    public class BlogPagingFilteringModel : BasePageableModel
    {
        #region Methods

        public virtual DateTime? GetParsedMonth()
        {
            DateTime? result = null;
            if (string.IsNullOrEmpty(Month)) return null;
            var tempDate = Month.Split(new [] { '-' });
            if (tempDate.Length != 2) return null;
            int.TryParse(tempDate[0], out var year);
            int.TryParse(tempDate[1], out var month);
            try
            {
                result = new DateTime(year, month, 1);
            }
            catch { }
            return result;
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
}