namespace Grand.Business.Core.Dto;

public class CountryStatesDto
{
    public string Country { get; set; }
    public string StateProvinceName { get; set; }
    public string Abbreviation { get; set; }
    public int DisplayOrder { get; set; }
    public bool Published { get; set; }
}