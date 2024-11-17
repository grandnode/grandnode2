using Grand.SharedKernel.Attributes;

namespace Grand.Domain;

public abstract class ParentEntity
{
    [DBFieldName("_id")]
    public string Id { get; set; } = UniqueIdentifier.New;
}