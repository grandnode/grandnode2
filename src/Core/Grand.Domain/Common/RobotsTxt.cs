namespace Grand.Domain.Common
{
    public partial class RobotsTxt : BaseEntity
    {

        /// <summary>
        /// Gets or sets the name of robots.txt
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text of robots.txt
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value store ident
        /// </summary>
        public string StoreId { get; set; }

    }
}
