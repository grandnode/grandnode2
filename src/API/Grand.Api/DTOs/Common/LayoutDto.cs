using Grand.Api.Models;

namespace Grand.Api.DTOs.Common
{
    public partial class LayoutDto : BaseApiEntityModel
    {
        public string Name { get; set; }
        public int DisplayOrder { get; set; }
        public string ViewPath { get; set; }
    }
}
