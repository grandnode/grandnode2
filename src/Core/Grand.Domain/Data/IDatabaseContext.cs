namespace Grand.Domain.Data
{
    public interface IDatabaseContext
    {
        void SetConnection(string connectionString);
        bool InstallProcessCreateTable { get; }
        bool InstallProcessCreateIndex { get; }
        Task<bool> DatabaseExist();
        IQueryable<T> Table<T>(string collectionName);
        Task CreateTable(string name, string collation);
        Task DeleteTable(string name);
        Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName, bool unique = false) where T : BaseEntity;
        Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity;
    }
}
