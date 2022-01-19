namespace Grand.Domain.Data
{
    public interface IStoreFilesContext
    {
        Task<byte[]> BucketDownload(string id);
        Task BucketDelete(string id);
        Task<string> BucketUploadFromBytes(string filename, byte[] source);
    }
}
