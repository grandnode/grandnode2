using Grand.Api.Models;

namespace Grand.Api.DTOs.Common;

public class CurrencyDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string CurrencyCode { get; set; }
    public double Rate { get; set; }
    public string DisplayLocale { get; set; }
    public string CustomFormatting { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}