namespace Grand.Domain;

public interface IAuditableEntity
{
    public DateTime CreatedOnUtc { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public string UpdatedBy { get; set; }
}