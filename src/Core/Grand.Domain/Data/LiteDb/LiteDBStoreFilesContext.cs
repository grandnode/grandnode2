using LiteDB;

namespace Grand.Domain.Data.LiteDb
{
    public class LiteDBStoreFilesContext : IStoreFilesContext
    {
        protected LiteDatabase _database;

        public LiteDBStoreFilesContext(LiteDatabase database)
        {
            _database = database;
        }

        public async Task<byte[]> BucketDownload(string id)
        {
            var fs = _database.FileStorage;
            var file = fs.FindById(id);

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using (var stream = file.OpenRead())
            using (MemoryStream mstream = new ())
            {
                stream.CopyTo(mstream);
                return await Task.FromResult(mstream.ToArray());
            }
        }
        public Task BucketDelete(string id)
        {
            var fs = _database.FileStorage;
            fs.Delete(id);

            return Task.CompletedTask;
        }

        public async Task<string> BucketUploadFromBytes(string filename, byte[] source)
        {
            var id = UniqueIdentifier.New;
            var fs = _database.FileStorage;
            fs.Upload(id, filename, new MemoryStream(source));

            return await Task.FromResult(id);
        }
    }
}
