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
    }
}
