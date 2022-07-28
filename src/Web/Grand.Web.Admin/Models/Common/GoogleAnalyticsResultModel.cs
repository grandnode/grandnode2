namespace Grand.Web.Admin.Models.Common
{
    public partial class GoogleAnalyticsResultModel
    {
        public GoogleAnalyticsResultModel()
        {
            Records = new List<Dictionary<string, string>>();
        }

        /// <summary>
        /// Key: metric/dimension header,
        /// Value: metric/dimension value
        /// </summary>
        public List<Dictionary<string, string>> Records { get; private set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Message { get; set; }
    }
}
