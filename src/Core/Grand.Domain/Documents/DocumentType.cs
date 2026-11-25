namespace Grand.Domain.Documents;

/// <summary>
///     Represents a document type
/// </summary>
public class DocumentType : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}