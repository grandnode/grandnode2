namespace Grand.Domain.Knowledgebase;

public interface ITreeNode
{
    string Id { get; set; }

    string Name { get; set; }

    string ParentCategoryId { get; set; }
}