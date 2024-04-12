namespace Grand.Domain.History;

public class HistoryObject : BaseEntity
{
    public BaseEntity Object { get; set; }
}