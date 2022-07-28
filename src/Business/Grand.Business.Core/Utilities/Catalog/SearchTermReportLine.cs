namespace Grand.Business.Core.Utilities.Catalog
{
    /// <summary>
    /// Search term (for statistics)
    /// </summary>
    public class SearchTermReportLine
    {
        /// <summary>
        /// Gets or sets the keyword
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// Gets or sets search count
        /// </summary>
        public int Count { get; set; }
    }
}
