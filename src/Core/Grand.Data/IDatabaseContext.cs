using Grand.Domain;

namespace Grand.Data;

public interface IDatabaseContext
{    
    Task<bool> DatabaseExist();   
    Task CreateTable(string name, string collation);
    Task DeleteTable(string name);
    Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName, bool unique = false)
        where T : BaseEntity;
    Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity;

    /// <summary>
    ///     Creates the text index. A collection holds one text index: a different one, under another name or with
    ///     other fields, makes it fail, so a changed field list takes a new name and a migration dropping the old
    ///     index. A no-op where the provider has no text indexes
    /// </summary>
    Task CreateTextIndex<T>(IRepository<T> repository, string[] fieldNames, string indexName,
        CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    ///     Whether the text index exists and answers queries - false while it is being built, when the check
    ///     itself fails and where the provider has no text indexes
    /// </summary>
    Task<bool> TextIndexExists<T>(IRepository<T> repository, string indexName,
        CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    ///     Gets the repository's table narrowed to documents with a whole word of the search through the text
    ///     index. Further filters go on top of it. Without text indexes the table comes back unchanged
    /// </summary>
    IQueryable<T> TextSearch<T>(IRepository<T> repository, string search) where T : BaseEntity;
}
