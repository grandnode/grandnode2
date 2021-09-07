using System.Threading.Tasks;

namespace Grand.Domain.Data
{
    public interface IStoreFilesContext
    {
        Task<byte[]> BucketDownload(string id);
        Task<string> BucketUploadFromBytesAsync(string filename, byte[] source);
    }
}
