using Grand.Api.Models;

namespace Grand.Api.DTOs.Catalog
{
	public partial class PageDto : BaseApiEntityModel
    {
        public PageDto()
        {
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string SystemName { get; set; }
        public string SeName { get; set; }
        public bool IncludeInSitemap { get; set; }
        public bool IncludeInMenu { get; set; }
    }
}
