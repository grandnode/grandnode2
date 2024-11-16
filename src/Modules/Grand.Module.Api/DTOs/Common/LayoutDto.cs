using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Common;

public class LayoutDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public string ViewPath { get; set; }
}