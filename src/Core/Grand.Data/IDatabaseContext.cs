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
}