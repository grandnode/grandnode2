using System.Linq;
using System.Threading.Tasks;

namespace Grand.Domain.Data
{
    public interface IDatabaseContext
    {
        IQueryable<T> Table<T>(string collectionName);
        Task<byte[]> GridFSBucketDownload(string id);
        Task<string> GridFSBucketUploadFromBytesAsync(string filename, byte[] source);
        Task<bool> DatabaseExist(string connectionString);
        Task CreateTable(string name, string collation);
        Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName, bool unique = false) where T : BaseEntity;
    }
}
