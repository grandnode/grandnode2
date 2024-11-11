namespace Grand.Domain.Common;

public class GrandNodeVersion : BaseEntity
{
    public string InstalledVersion { get; set; }
    public string DataBaseVersion { get; set; }
}